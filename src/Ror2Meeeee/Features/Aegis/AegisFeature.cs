using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Aegis;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // "Égide Vampírica" (verde) e "Égide do Devorador" (void, corrompe a primeira). Gatilho: um ataque BLOQUEADO.
    // Bloqueio = o jogo rejeitou o dano por Tougher Times, Safer Spaces, parry, armadura do Captain ou pela chance própria
    // da Égide (5%, não acumula). Rejeição por imunidade/invencibilidade (i-frames, Immune, veículo, escudo do trono) não conta:
    // o estado é lido ANTES do dano, porque o jogo usa a mesma flag (DamageInfo.rejected) para tudo.
    // Verde: rouba % da vida atual do agressor e cura o dono. Void: onda que rouba % da vida atual de cada inimigo no raio.
    // Só no host (TakeDamage é servidor). Passos 1 (sorteio/elegibilidade) e 4 (roubo) do BodyHooks.TakeDamageProcess,
    // hook único que só existe enquanto alguém tem uma Égide (ou o Coração Pesado).
    public sealed class AegisFeature : Feature, IHolderSet
    {
        public const string ItemName = "Ror2MeeeeeVampiricAegis";
        public const string Token = "ROR2MEEEEE_ITEM_VAMPIRICAEGIS";
        public const string VoidName = "Ror2MeeeeeDevourerAegis";
        public const string VoidToken = "ROR2MEEEEE_ITEM_DEVOURERAEGIS";

        private static ItemDef _def, _voidDef;
        private static ItemIndex _index = ItemIndex.None, _voidIndex = ItemIndex.None;
        private static GameObject _blockFx, _waveFx;

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

        // Quem tem uma das Égides, por GameObject do corpo (o HealthComponent está no mesmo objeto).
        private readonly Dictionary<GameObject, CharacterBody> _holders = new Dictionary<GameObject, CharacterBody>();

        public bool HasHolders => _holders.Count > 0;
        public bool Holds(GameObject body) => _holders.ContainsKey(body);
        private static readonly TeamIndex[] EnemyTeams = { TeamIndex.Monster, TeamIndex.Void, TeamIndex.Lunar };

        public static void Register(ManualLogSource log)
        {
            System.Globalization.CultureInfo en = Loc.EnCulture, pt = Loc.PtCulture;
            string P(float f, System.Globalization.CultureInfo c) => (f * 100f).ToString("0.##", c) + "%";
            string M(float f, System.Globalization.CultureInfo c) => f.ToString("0.#", c) + " m";
            var chanceEn = $"Grants a <style=cIsUtility>{AegisRules.BlockChancePercent.ToString("0.#", en)}% chance to block</style> an attack " +
                           "<style=cStack>(does not stack)</style>; any other block also counts (Tougher Times, Safer Spaces, parry...). ";
            var chancePt = $"Dá <style=cIsUtility>{AegisRules.BlockChancePercent.ToString("0.#", pt)}% de chance de bloquear</style> um ataque " +
                           "<style=cStack>(não acumula)</style>; vale também qualquer outro bloqueio (Tougher Times, Safer Spaces, parry...). ";

            _def = ModItem.Register(ItemName, Token, ItemTier.Tier2, new[] { ItemTag.Healing, ItemTag.Utility, ItemTag.AIBlacklist },
                "egide_vampirica.png", new Color32(180, 30, 40, 255),
                ("Vampiric Aegis",
                 "Dare to hurt me, and you'll pay with your own blood.",
                 chanceEn + $"On block, <style=cIsHealing>steal {P(AegisRules.StealPerStack, en)} of the attacker's current health</style> " +
                 $"<style=cStack>(+{P(AegisRules.StealPerStack, en)} per stack, up to {P(AegisRules.StealCap, en)})</style>, restoring your health. " +
                 "Only works on <style=cIsUtility>blocked</style> attacks.",
                 "The knight did not fear being struck. Every blow he took only prolonged his life, and shortened the life of whoever dared challenge him."),
                ("Égide Vampírica",
                 "Se ousar me ferir, pagará com o próprio sangue.",
                 chancePt + $"Ao bloquear, <style=cIsHealing>roube {P(AegisRules.StealPerStack, pt)} da vida atual do agressor</style> " +
                 $"<style=cStack>(+{P(AegisRules.StealPerStack, pt)} por acúmulo, até {P(AegisRules.StealCap, pt)})</style>, restaurando a sua vida. " +
                 "Só funciona em ataques <style=cIsUtility>bloqueados</style>.",
                 "O cavaleiro não temia ser atingido. Cada golpe recebido apenas prolongava sua vida — e encurtava a de quem ousasse desafiá-lo."),
                log, "RoR2/Base/SprintArmor/PickupBuckler.prefab");
            _voidDef = ModItem.Register(VoidName, VoidToken, ItemTier.VoidTier2, new[] { ItemTag.Healing, ItemTag.Utility, ItemTag.AIBlacklist },
                "egide_devorador.png", new Color32(150, 60, 200, 255),
                ("Devourer's Aegis",
                 "No matter who attacks me. Everyone will pay. <style=cIsVoid>Corrupts all Vampiric Aegises.</style>",
                 chanceEn + $"On block, release a Void wave that <style=cIsHealing>steals {P(AegisRules.VoidPerStack, en)} of the current health</style> of every enemy " +
                 $"within <style=cIsUtility>{M(AegisRules.VoidBaseRadius, en)}</style> <style=cStack>(+{P(AegisRules.VoidPerStack, en)} and +{M(AegisRules.VoidRadiusPerStack, en)} " +
                 $"per stack, up to {P(AegisRules.VoidCap, en)} and {M(AegisRules.VoidRadiusCap, en)})</style>, restoring your health by the total drained. " +
                 "<style=cIsVoid>Corrupts all Vampiric Aegises.</style>",
                 "The first attacker offered his blood. The Void found the offer lacking and summoned everyone present to pay the difference."),
                ("Égide do Devorador",
                 "Não importa quem me ataque. Todos pagarão. <style=cIsVoid>Corrompe todas as Égides Vampíricas.</style>",
                 chancePt + $"Ao bloquear, libere uma onda do Vazio que <style=cIsHealing>rouba {P(AegisRules.VoidPerStack, pt)} da vida atual</style> de cada inimigo " +
                 $"num raio de <style=cIsUtility>{M(AegisRules.VoidBaseRadius, pt)}</style> <style=cStack>(+{P(AegisRules.VoidPerStack, pt)} e +{M(AegisRules.VoidRadiusPerStack, pt)} " +
                 $"por acúmulo, até {P(AegisRules.VoidCap, pt)} e {M(AegisRules.VoidRadiusCap, pt)})</style>, restaurando a sua vida pela soma drenada. " +
                 "<style=cIsVoid>Corrompe todas as Égides Vampíricas.</style>",
                 "O primeiro agressor ofereceu seu sangue. O Vazio considerou a oferta insuficiente e convocou todos os presentes para pagar a diferença."),
                log, "RoR2/DLC1/BearVoid/PickupBearVoid.prefab");
            ModItem.Corrupt(_def, _voidDef, log);
            if (_def) _def.pickupModelPrefab = ShieldModel(ItemName, new Color32(120, 10, 20, 255), new Color32(200, 160, 60, 255), Color.black, log) ?? _def.pickupModelPrefab;
            if (_voidDef) _voidDef.pickupModelPrefab = ShieldModel(VoidName, new Color32(50, 15, 80, 255), new Color32(150, 70, 220, 255), new Color(0.5f, 0.15f, 0.8f), log) ?? _voidDef.pickupModelPrefab;
            _blockFx = Load("RoR2/Base/Bear/BearProc.prefab", log);
            _waveFx = Load("RoR2/DLC1/ExplodeOnDeathVoid/ExplodeOnDeathVoidExplosionEffect.prefab", log);
        }

        // Escudo redondo de revolução: verso plano, borda grossa, frente em domo raso e umbo no centro.
        // Perfil (raio, altura) do centro do verso até o topo do umbo; V escolhe a faixa (0,25 = corpo, 0,75 = metal).
        private static readonly Vector2[] ShieldProfile =
        {
            new Vector2(0f, -0.03f), new Vector2(0.46f, -0.03f), new Vector2(0.5f, -0.02f), new Vector2(0.52f, 0f),
            new Vector2(0.52f, 0.05f), new Vector2(0.49f, 0.07f), new Vector2(0.46f, 0.05f), new Vector2(0.3f, 0.08f),
            new Vector2(0.14f, 0.1f), new Vector2(0.12f, 0.11f), new Vector2(0.11f, 0.15f), new Vector2(0.07f, 0.19f), new Vector2(0f, 0.2f),
        };
        private static readonly float[] ShieldV = { 0.25f, 0.25f, 0.75f, 0.75f, 0.75f, 0.75f, 0.75f, 0.25f, 0.25f, 0.75f, 0.75f, 0.75f, 0.75f };

        private static GameObject ShieldModel(string name, Color32 body, Color32 metal, Color emission, ManualLogSource log)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            // Em pé, frente para +Z: o pickup gira em torno do eixo vertical.
            ProcModel.AddLathe(ShieldProfile, ShieldV, 32, Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f)), verts, uvs, tris);
            var tex = ProcModel.Bands(name + "Tex",
                (body, Color32.Lerp(body, new Color32(0, 0, 0, 255), 0.35f)),
                (metal, Color32.Lerp(metal, new Color32(255, 255, 255, 255), 0.25f)));
            return ProcModel.Build(name, verts, uvs, tris, tex, 0.4f, emission, log);
        }

        private static GameObject Load(string key, ManualLogSource log)
        {
            try { return Addressables.LoadAssetAsync<GameObject>(key).WaitForCompletion(); }
            catch (Exception e) { log.LogWarning($"Aegis: efeito {key} indisponível ({e.GetType().Name})."); return null; }
        }

        public AegisFeature(Cfg cfg, ManualLogSource log) : base("Aegis", cfg, log,
            "Vampiric Aegis (green) and Devourer's Aegis (void) items: 5% chance to block; on block, they steal health from the attacker (green) or from all nearby enemies (void). Off: the items exist but do nothing.",
            "Itens Égide Vampírica (verde) e Égide do Devorador (void): 5% de chance de bloquear; ao bloquear, roubam vida do agressor (verde) ou de todos os inimigos próximos (void). Desligado: os itens existem mas não fazem nada.",
            enabledByDefault: true)
        {
        }

        public override void ResetRunState()
        {
            _holders.Clear();
            SetHook(false);
        }

        protected override void OnEnable()
        {
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += OnBodyDestroy);
            BodyHooks.Aegis = this; // TakeDamageProcess: hook único em BodyHooks (passos 1 e 4)
        }

        protected override void OnDisable()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyDestroyGlobal -= OnBodyDestroy;
            _holders.Clear();
            SetHook(false);
        }

        private static void SetHook(bool on) => BodyHooks.Sync();

        private static int Count(Inventory inv, ItemIndex idx) => inv && idx != ItemIndex.None ? inv.GetItemCountEffective(idx) : 0;

        private void OnHolderMaybeChanged(CharacterBody body)
        {
            try
            {
                if (!NetworkServer.active || !body) return;
                var inv = body.inventory;
                if (Count(inv, Index) > 0 || Count(inv, VoidIndex) > 0) _holders[body.gameObject] = body;
                else if (_holders.Count > 0) _holders.Remove(body.gameObject);
                SetHook(_holders.Count > 0);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBodyDestroy(CharacterBody body)
        {
            if (_holders.Count > 0 && body && _holders.Remove(body.gameObject)) SetHook(_holders.Count > 0);
        }

        // Passo 1 do BodyHooks.TakeDamageProcess: o golpe pode virar bloqueio? Rejeição por imunidade/invencibilidade
        // não conta (lido ANTES do jogo processar). Sorteia o bloqueio próprio (5%). true = observar o resultado (passo 4).
        internal bool BeforeDamage(HealthComponent self, DamageInfo damageInfo, out CharacterBody body)
        {
            body = null;
            try
            {
                if (_holders.Count == 0 || !self || damageInfo == null || damageInfo.rejected) return false;
                if ((damageInfo.damageType & DamageType.BypassBlock) != 0) return false; // como o Tougher Times do jogo
                if (!_holders.TryGetValue(self.gameObject, out body) || !body) return false;
                var eligible = !body.HasBuff(RoR2Content.Buffs.HiddenInvincibility) && !body.HasBuff(RoR2Content.Buffs.Immune)
                               && !body.HasBuff(DLC2Content.Buffs.SojournVehicle) && !body.HasBuff(RoR2Content.Buffs.BrotherThroneShield);
                if (eligible && damageInfo.damage > 0f && Util.CheckRoll(AegisRules.BlockChancePercent, body.master))
                {
                    damageInfo.rejected = true; // bloqueio próprio; o jogo segue e chama os receptores de rejeição
                    if (_blockFx)
                        EffectManager.SpawnEffect(_blockFx, new EffectData
                        {
                            origin = damageInfo.position,
                            rotation = Util.QuaternionSafeLookRotation(damageInfo.force != Vector3.zero ? damageInfo.force : UnityEngine.Random.onUnitSphere),
                        }, transmit: true);
                }
                return eligible;
            }
            catch (Exception e)
            {
                Fail(e);
                return false;
            }
        }

        // Passo 4 do BodyHooks.TakeDamageProcess: o golpe foi bloqueado.
        internal void AfterBlocked(CharacterBody body, DamageInfo damageInfo)
        {
            try { OnBlocked(body, damageInfo); }
            catch (Exception e) { Fail(e); }
        }

        private static void OnBlocked(CharacterBody body, DamageInfo damageInfo)
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive) return;
            var inv = body.inventory;
            var green = Count(inv, Index);
            var voidStacks = Count(inv, VoidIndex);
            var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
            var healed = 0f;

            var attacker = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
            if (green > 0 && attacker && attacker != body && IsEnemy(attacker, team))
                healed += Drain(body, attacker, AegisRules.StealFraction(green), DamageColorIndex.Bleed);

            if (voidStacks > 0)
            {
                var radius = AegisRules.VoidRadius(voidStacks);
                var fraction = AegisRules.VoidFraction(voidStacks);
                var center = body.corePosition;
                var r2 = radius * radius;
                foreach (var t in EnemyTeams)
                {
                    if (t == team) continue;
                    var members = TeamComponent.GetTeamMembers(t);
                    for (var i = members.Count - 1; i >= 0; i--) // Drain pode matar e tirar da lista
                    {
                        var tc = members[i];
                        var b = tc ? tc.body : null;
                        if (b && (b.corePosition - center).sqrMagnitude <= r2)
                            healed += Drain(body, b, fraction, DamageColorIndex.Void);
                    }
                }
                if (_waveFx)
                    EffectManager.SpawnEffect(_waveFx, new EffectData { origin = center, scale = radius, rotation = Quaternion.identity }, transmit: true);
            }

            if (healed > 0f) body.healthComponent.Heal(healed, default(ProcChainMask));
        }

        private static bool IsEnemy(CharacterBody other, TeamIndex team)
        {
            var t = other.teamComponent ? other.teamComponent.teamIndex : TeamIndex.None;
            return t != team && t != TeamIndex.None;
        }

        // Tira `fraction` da vida atual do alvo (sem armadura nem procs) e devolve quanto ele perdeu de fato
        // (o alvo pode bloquear ou ter escudo/barreira absorvendo).
        private static float Drain(CharacterBody owner, CharacterBody target, float fraction, DamageColorIndex color)
        {
            var hc = target.healthComponent;
            if (fraction <= 0f || !hc || !hc.alive) return 0f;
            var amount = hc.health * fraction;
            if (amount <= 0f) return 0f;
            var before = hc.combinedHealth;
            hc.TakeDamage(new DamageInfo
            {
                damage = amount,
                attacker = owner.gameObject,
                inflictor = owner.gameObject,
                position = target.corePosition,
                procCoefficient = 0f,
                crit = false,
                damageType = (DamageTypeCombo)(DamageType.BypassArmor | DamageType.BypassBlock),
                damageColorIndex = color,
                procChainMask = default(ProcChainMask),
            });
            var lost = before - (hc ? hc.combinedHealth : 0f);
            return lost > 0f ? lost : 0f;
        }
    }
}
