using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using RoR2;
using Ror2Meeeee.Core.RustyNail;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // "Pilha de Pregos Enferrujados" (branco) e "Pilha de Espinhos do Vazio" (void, corrompe a primeira).
    // Gatilho: golpe com coeficiente de proc > 0 (GlobalEventManager.onServerDamageDealt, só no host).
    // Branco: prego de 10% (+por cópia) + sangramento. Void: espinho de 10% (+por cópia) + Estilhaço do Vazio; 3 s depois, detonação de 250% e
    // chuva de espinhos no raio que marca os atingidos (cadeia limitada por MaxChain e por MaxActiveMarks).
    // O dano do mod usa procCoefficient 0: não dispara procs, nem este item de novo (sem laço).
    public sealed class RustyNailFeature : Feature
    {
        public const string ItemName = "Ror2MeeeeeRustyNailStack";
        public const string Token = "ROR2MEEEEE_ITEM_RUSTYNAILSTACK";
        public const string VoidName = "Ror2MeeeeeVoidSpikeStack";
        public const string VoidToken = "ROR2MEEEEE_ITEM_VOIDSPIKESTACK";

        private const int MaxEffectsPerFrame = 16; // efeitos visuais transmitidos por frame; o dano não é cortado

        private static ItemDef _def, _voidDef;
        private static ItemIndex _index = ItemIndex.None, _voidIndex = ItemIndex.None;
        private static BuffDef _shard;
        private static GameObject _nailTracer, _voidTracer, _detonateFx, _burstFx;
        private static readonly TeamIndex[] EnemyTeams = { TeamIndex.Player, TeamIndex.Monster, TeamIndex.Void, TeamIndex.Lunar };

        private static ItemIndex Index
        {
            get
            {
                if (_index == ItemIndex.None && _def) _index = _def.itemIndex;
                return _index;
            }
        }

        private static ItemIndex VoidIndex
        {
            get
            {
                if (_voidIndex == ItemIndex.None && _voidDef) _voidIndex = _voidDef.itemIndex;
                return _voidIndex;
            }
        }

        public static void Register(ManualLogSource log)
        {
            var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            string P(float f) => (f * 100f).ToString("0.##", pt) + "%";
            string Chance(float perStack) => $"Seus ataques têm <style=cIsDamage>{RustyNailRules.ChanceBase.ToString("0.#", pt)}% de chance</style> " +
                         $"<style=cStack>(+{perStack.ToString("0.#", pt)}% por acúmulo)</style> de disparar ";
            var chance = Chance(RustyNailRules.ChancePerStack);

            _def = ModItem.Register(ItemName, Token, ItemTier.Tier1, new[] { ItemTag.Damage },
                "prego.png", new Color32(150, 80, 40, 255),
                "Pilha de Pregos Enferrujados",
                "Chance de disparar um prego que causa sangramento.",
                chance + $"um prego que causa <style=cIsDamage>{P(RustyNailRules.NailBase)} de dano base</style> <style=cStack>(+{P(RustyNailRules.NailPerStack)} por acúmulo)</style> e aplica " +
                "<style=cIsDamage>sangramento</style>.",
                "Ninguém sabe de onde vieram. Ninguém quer saber onde eles estiveram.",
                log, "RoR2/Base/Items/BleedOnHit/PickupTriTip.prefab");
            _voidDef = ModItem.Register(VoidName, VoidToken, ItemTier.VoidTier1, new[] { ItemTag.Damage, ItemTag.AIBlacklist },
                "prego_void.png", new Color32(150, 60, 200, 255),
                "Pilha de Espinhos do Vazio",
                "Espinhos que marcam e detonam inimigos. <style=cIsVoid>Corrompe todas as Pilhas de Pregos Enferrujados.</style>",
                Chance(RustyNailRules.VoidChancePerStack) + $"um espinho do Vazio que causa <style=cIsDamage>{P(RustyNailRules.NailBase)} de dano base</style> <style=cStack>(+{P(RustyNailRules.VoidNailPerStack)} por acúmulo)</style> e marca o alvo com " +
                $"<style=cIsVoid>Estilhaço do Vazio</style> por {RustyNailRules.MarkDuration.ToString("0.#", pt)} s. Ao fim, o alvo sofre uma detonação de " +
                $"<style=cIsDamage>{P(RustyNailRules.DetonationCoefficient)} de dano base</style> e dispara espinhos em todas as direções " +
                $"(raio de {RustyNailRules.BurstRadius.ToString("0.#", pt)} m), causando <style=cIsDamage>{P(RustyNailRules.BurstBase)}</style> " +
                $"<style=cStack>(+{P(RustyNailRules.BurstPerStack)} por acúmulo)</style> e marcando os atingidos. " +
                "<style=cIsVoid>Corrompe todas as Pilhas de Pregos Enferrujados.</style>",
                "Os pregos não enferrujaram. Algo os comeu por dentro e deixou só a forma, faminta.",
                log, "RoR2/DLC1/Items/BleedOnHitVoid/PickupTriTipVoid.prefab");
            ModItem.Corrupt(_def, _voidDef, log);
#pragma warning disable CS0618 // pickupModelPrefab: campo "left for modders' use" no 1.5.0
            // Modelo próprio; se não gerar, fica a Tri-Tip carregada acima.
            if (_def) _def.pickupModelPrefab = NailModel.Build(ItemName, new Color32(95, 45, 20, 255), new Color32(175, 90, 40, 255), Color.black, log) ?? _def.pickupModelPrefab;
            if (_voidDef) _voidDef.pickupModelPrefab = NailModel.Build(VoidName, new Color32(45, 15, 70, 255), new Color32(140, 60, 200, 255), new Color(0.5f, 0.15f, 0.8f), log) ?? _voidDef.pickupModelPrefab;
#pragma warning restore CS0618

            try
            {
                var buff = ScriptableObject.CreateInstance<BuffDef>();
                buff.name = "Ror2MeeeeeVoidShard";
                buff.isDebuff = true;
                buff.canStack = false;
                buff.buffColor = new Color32(180, 90, 255, 255);
                var fracture = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/Items/BleedOnHitVoid/bdFracture.asset").WaitForCompletion();
                if (fracture) buff.iconSprite = fracture.iconSprite;
                if (ContentAddition.AddBuffDef(buff)) _shard = buff;
                else log.LogWarning("RustyNail: ContentAddition recusou o buff; a marca fica invisível.");
            }
            catch (Exception e) { log.LogWarning($"RustyNail: buff do Estilhaço indisponível ({e.GetType().Name}); a marca fica invisível."); }

            _nailTracer = Load("RoR2/Base/Characters/Toolbot/Skills/TracerToolbotNails.prefab", log);
            _voidTracer = Load("RoR2/DLC1/Characters/VoidSurvivor/Skills/Primary/Base/VoidSurvivorBeamTracer.prefab", log);
            _detonateFx = Load("RoR2/DLC1/Items/BleedOnHitVoid/FractureImpactEffect.prefab", log);
            _burstFx = Load("RoR2/DLC1/ExplodeOnDeathVoid/ExplodeOnDeathVoidExplosionEffect.prefab", log);
        }

        private static GameObject Load(string key, ManualLogSource log)
        {
            try { return Addressables.LoadAssetAsync<GameObject>(key).WaitForCompletion(); }
            catch (Exception e) { log.LogWarning($"RustyNail: efeito {key} indisponível ({e.GetType().Name})."); return null; }
        }

        private struct Mark
        {
            public float Due;
            public GameObject Attacker;
            public TeamIndex Team;
            public float BaseDamage; // dano base do atacante no momento da marca (ele pode morrer antes da detonação)
            public int Stacks;
            public int Generation;
        }

        private readonly ConfigEntry<int> _maxChain;
        private int _maxChainValue;
        private readonly Dictionary<HealthComponent, Mark> _marks = new Dictionary<HealthComponent, Mark>();
        private readonly List<HealthComponent> _due = new List<HealthComponent>(); // reutilizada: sem alocação por tick
        private int _frame = -1;
        private int _effectsThisFrame;

        public RustyNailFeature(Cfg cfg, ManualLogSource log) : base("RustyNail", cfg, log,
            "Itens Pilha de Pregos Enferrujados (branco) e Pilha de Espinhos do Vazio (void): chance por golpe de disparar um prego com sangramento ou um espinho que marca e detona. Só no host. Desligado: os itens existem mas não fazem nada.",
            enabledByDefault: true)
        {
            _maxChain = cfg.Bind("RustyNail", "MaxChain", 2,
                "Quantas vezes a chuva de espinhos do Vazio pode marcar de novo em cadeia (marca -> detonação -> marca...). 0 = a chuva causa dano mas não marca.");
            _maxChainValue = _maxChain.Value;
            _maxChain.SettingChanged += (_, __) => _maxChainValue = _maxChain.Value;
        }

        public override void ResetRunState() => _marks.Clear();

        protected override void OnEnable()
        {
            Track("GlobalEventManager.onServerDamageDealt", () => GlobalEventManager.onServerDamageDealt += OnDamageDealt);
            Track("RoR2Application.onUpdate", () => RoR2Application.onUpdate += OnUpdate);
        }

        protected override void OnDisable()
        {
            GlobalEventManager.onServerDamageDealt -= OnDamageDealt;
            RoR2Application.onUpdate -= OnUpdate;
            _marks.Clear();
        }

        private static int Count(Inventory inv, ItemIndex idx) => inv && idx != ItemIndex.None ? inv.GetItemCountEffective(idx) : 0;

        // Todo golpe no host passa aqui: sem item, custa 2 leituras de inventário.
        private void OnDamageDealt(DamageReport report)
        {
            try
            {
                var info = report.damageInfo;
                var attacker = report.attackerBody;
                if (info == null || info.procCoefficient <= 0f || !attacker || !report.victim || !report.victimBody) return;
                var inv = attacker.inventory;
                var white = Count(inv, Index);
                var voidStacks = Count(inv, VoidIndex);
                if (white == 0 && voidStacks == 0) return;

                var victim = report.victim;
                var master = report.attackerMaster;
                var from = attacker.corePosition;
                var to = report.victimBody.corePosition;

                if (white > 0 && Util.CheckRoll(RustyNailRules.ChancePercent(white, RustyNailRules.ChancePerStack) * info.procCoefficient, master))
                {
                    Hit(victim, attacker.gameObject, attacker.damage * RustyNailRules.NailCoefficient(white, RustyNailRules.NailPerStack), info.crit, DamageColorIndex.Bleed);
                    if (victim.alive)
                        DotController.InflictDot(victim.gameObject, attacker.gameObject, info.inflictedHurtbox, default(ProcChainMask),
                            DotController.DotIndex.Bleed, RustyNailRules.BleedDuration * info.procCoefficient);
                    Tracer(_nailTracer, from, to);
                    if (Cfg.VerboseLogging) Log.LogDebug($"RustyNail: prego em {report.victimBody.GetDisplayName()} ({white} cópias, {attacker.damage * RustyNailRules.NailCoefficient(white, RustyNailRules.NailPerStack):0.#} de dano + sangramento)");
                }

                if (voidStacks > 0 && Util.CheckRoll(RustyNailRules.ChancePercent(voidStacks, RustyNailRules.VoidChancePerStack) * info.procCoefficient, master))
                {
                    Hit(victim, attacker.gameObject, attacker.damage * RustyNailRules.NailCoefficient(voidStacks, RustyNailRules.VoidNailPerStack), info.crit, DamageColorIndex.Void);
                    TryMark(victim, attacker.gameObject, report.attackerTeamIndex, attacker.damage, voidStacks, 0);
                    Tracer(_voidTracer, from, to);
                    if (Cfg.VerboseLogging) Log.LogDebug($"RustyNail: espinho do Vazio em {report.victimBody.GetDisplayName()} ({voidStacks} cópias, marcas ativas: {_marks.Count})");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Marca já ativa não é renovada: a detonação não pode ser adiada para sempre.
        private void TryMark(HealthComponent victim, GameObject attacker, TeamIndex team, float baseDamage, int stacks, int generation)
        {
            if (!victim || !victim.alive || _marks.Count >= RustyNailRules.MaxActiveMarks || _marks.ContainsKey(victim)) return;
            _marks[victim] = new Mark
            {
                Due = Time.time + RustyNailRules.MarkDuration,
                Attacker = attacker,
                Team = team,
                BaseDamage = baseDamage,
                Stacks = stacks,
                Generation = generation,
            };
            if (_shard && victim.body) victim.body.AddTimedBuff(_shard, RustyNailRules.MarkDuration);
        }

        private void OnUpdate()
        {
            if (_marks.Count == 0 || !NetworkServer.active) return;
            try
            {
                var now = Time.time;
                foreach (var kv in _marks)
                    if (now >= kv.Value.Due) _due.Add(kv.Key);
                if (_due.Count == 0) return;
                // Detonar marca novos alvos em _marks: por isso a coleta antes.
                foreach (var hc in _due)
                {
                    var mark = _marks[hc];
                    _marks.Remove(hc);
                    Detonate(hc, mark);
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            finally
            {
                _due.Clear();
            }
        }

        private void Detonate(HealthComponent hc, Mark mark)
        {
            if (!hc || !hc.alive || !hc.body) return; // morreu antes: sem detonação nem chuva
            var center = hc.body.corePosition;
            var attacker = mark.Attacker ? mark.Attacker : null;
            Hit(hc, attacker, mark.BaseDamage * RustyNailRules.DetonationCoefficient, false, DamageColorIndex.Void);
            Effect(_detonateFx, new EffectData { origin = center, rotation = Quaternion.identity });
            if (Cfg.VerboseLogging) Log.LogDebug($"RustyNail: detonação em {hc.body.GetDisplayName()} (geração {mark.Generation})");

            var burst = mark.BaseDamage * RustyNailRules.BurstCoefficient(mark.Stacks);
            var spread = RustyNailRules.CanSpread(mark.Generation, _maxChainValue);
            var r2 = RustyNailRules.BurstRadius * RustyNailRules.BurstRadius;
            foreach (var t in EnemyTeams)
            {
                if (t == mark.Team) continue;
                var members = TeamComponent.GetTeamMembers(t);
                for (var i = members.Count - 1; i >= 0; i--) // Hit pode matar e tirar da lista
                {
                    var tc = members[i];
                    var b = tc ? tc.body : null;
                    var other = b ? b.healthComponent : null;
                    if (!other || other == hc || (b.corePosition - center).sqrMagnitude > r2) continue;
                    Hit(other, attacker, burst, false, DamageColorIndex.Void);
                    if (spread) TryMark(other, attacker, mark.Team, mark.BaseDamage, mark.Stacks, mark.Generation + 1);
                }
            }
            Effect(_burstFx, new EffectData { origin = center, scale = RustyNailRules.BurstRadius, rotation = Quaternion.identity });
        }

        private static void Hit(HealthComponent target, GameObject attacker, float damage, bool crit, DamageColorIndex color)
        {
            if (damage <= 0f || !target || !target.alive) return;
            target.TakeDamage(new DamageInfo
            {
                damage = damage,
                attacker = attacker,
                inflictor = attacker,
                position = target.body ? target.body.corePosition : target.transform.position,
                procCoefficient = 0f, // sem procs: nem este item nem outros reagem ao prego/espinho
                crit = crit,
                damageType = (DamageTypeCombo)DamageType.Generic,
                damageColorIndex = color,
                procChainMask = default(ProcChainMask),
            });
        }

        private void Tracer(GameObject prefab, Vector3 from, Vector3 to) =>
            Effect(prefab, new EffectData { origin = to, start = from });

        // Efeitos transmitidos a todos os clientes: teto por frame para 100% de chance + velocidade de ataque alta.
        private void Effect(GameObject prefab, EffectData data)
        {
            if (!prefab) return;
            var frame = Time.frameCount;
            if (frame != _frame) { _frame = frame; _effectsThisFrame = 0; }
            if (++_effectsThisFrame > MaxEffectsPerFrame) return;
            EffectManager.SpawnEffect(prefab, data, transmit: true);
        }
    }
}
