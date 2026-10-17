using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using RoR2;
using Ror2Meeeee.Core.Elites;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

#pragma warning disable CS0618 // pickupIconSprite/pickupModelPrefab: campos "left for modders' use" no 1.5.0

namespace Ror2Meeeee
{
    // Elites novos (reescrita do MoreElites 0.9.5 e do BlightedElites 1.2.2), só com assets do jogo:
    //  - Volátil (T1): todo golpe explode como um Brilliant Behemoth e dispara um míssil a cada 1-5 s.
    //  - Frenético (T1): o EliteYellowEquipment do jogo (o RecalculateStats já dá +2 de velocidade base e +50% de
    //    velocidade de ataque) e, se for IA, um blink até perto do alvo a cada 10 s. Usado por jogador: blink de 25 m.
    //  - Fortalecedor (T1): aura de Warbanner de 25 m para os aliados (o ward da Celestine com o buff trocado).
    //  - Arruinado (T2): dois afixos T1 aleatórios e diferentes.
    //  - Eco (T2): o edEcho do jogo (clones com 10% de vida que atiram projéteis caçadores), que o jogo não sorteia.
    // Correções em relação aos originais: tudo roda só no host (mísseis e blink não duplicam nos clientes), o blink do
    // jogador não cai em (0,0,0), um EliteDef por tier (o de Honra tem os stats do jogo para Honra, não os do T1), o
    // Volátil não dá um Behemoth que nunca sai, o Fortalecedor destrói só o ward dele e os tiers vêm do R2API.Elites,
    // não de índices fixos. Stats copiados dos elites do jogo (Flamejante, Flamejante de Honra, Malaquita).
    // O conteúdo (buffs, equipamentos, elites) é registrado sempre, para os catálogos baterem entre host e clientes;
    // a config só decide o que o diretor sorteia (host).
    public sealed class ElitesFeature : Feature
    {
        private const float DropChance = 0.00025f; // aspectos do jogo: 1 em 4000
        private const float BlinkInterval = 10f;
        private const float BlinkRange = 50f;
        private const float BlinkSpread = 15f;
        private const float PlayerBlinkDistance = 25f;
        private const float EmpoweringRadius = 25f;
        private const float BlightedHealth = 16f, BlightedDamage = 4f; // abaixo do T2 do jogo (18/6): já tem dois afixos

        private sealed class Placement
        {
            public string Key;
            public EliteDef Def;
            public List<CombatDirector.EliteTierDef> Tiers;
        }

        private sealed class Timed
        {
            public CharacterBody Body;
            public float Next;
        }

        private static readonly List<Placement> Placements = new List<Placement>();
        private static BuffDef _volatile, _frenzied, _empowering, _blighted;
        private static EquipmentDef _frenziedEquip;
        private static GameObject _missile, _explosion, _blink, _ward;
        private static readonly List<EliteDef> BlightedPool = new List<EliteDef>();

        private readonly Dictionary<string, ConfigEntry<bool>> _enabledByKey = new Dictionary<string, ConfigEntry<bool>>();
        private readonly List<Timed> _missiles = new List<Timed>();
        private readonly Dictionary<GameObject, CharacterBody> _volatileByObject = new Dictionary<GameObject, CharacterBody>();
        private readonly List<Timed> _blinkers = new List<Timed>();
        private readonly Dictionary<CharacterBody, GameObject> _wards = new Dictionary<CharacterBody, GameObject>();
        private readonly Dictionary<CharacterBody, BuffDef[]> _blightedAffixes = new Dictionary<CharacterBody, BuffDef[]>();
        private readonly List<BuffDef> _pool = new List<BuffDef>();
        private Xoroshiro128Plus _rng; // do mod: não consome RNG da run

        private static T Load<T>(string key) => Addressables.LoadAssetAsync<T>(key).WaitForCompletion();

        // Chamar no Awake do plugin (antes dos catálogos; o R2API.Elites já copiou os tiers do jogo). Falha não derruba o plugin.
        public static void Register(ManualLogSource log)
        {
            try
            {
                var t1 = Load<EliteDef>("RoR2/Base/EliteFire/edFire.asset");
                var t1Honor = Load<EliteDef>("RoR2/Base/EliteFire/edFireHonor.asset");
                var t2 = Load<EliteDef>("RoR2/Base/ElitePoison/edPoison.asset");
                var icon = Load<Sprite>("RoR2/DLC1/EliteEarth/texAffixEarthIcon.png");
                var buffIcon = Load<Sprite>("RoR2/Base/EliteIce/texBuffAffixWhite.tif");
                var model = ModItem.Model("RoR2/Base/EliteFire/PickupEliteFire.prefab", "Elites", log);
                _missile = Load<GameObject>("RoR2/Base/Common/MissileProjectile.prefab");
                _explosion = Load<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab");
                _blink = Load<GameObject>("RoR2/Junk/Treebot/SonicBoomEffect.prefab");
                _ward = Load<GameObject>("RoR2/Base/EliteHaunted/AffixHauntedWard.prefab");

                var red = new Color32(255, 70, 30, 255);
                _volatile = Buff("Volatile", red, buffIcon);
                var volatileEquip = Aspect("Volatile", _volatile, icon, model, "Aspecto Volátil", "Seus ataques explodem.",
                    "Todo golpe <style=cIsDamage>explode</style> em <style=cIsDamage>4 m</style> por <style=cIsDamage>60%</style> do dano e, a cada 1-5 s, você dispara um <style=cIsDamage>míssil</style>.");
                AddT1("Volatile", volatileEquip, red, "{0} Volátil", "RoR2/DLC1/Common/ColorRamps/texRampStrongerBurn.png", t1, t1Honor, log);

                var yellow = new Color32(255, 220, 40, 255);
                _frenziedEquip = Load<EquipmentDef>("RoR2/Junk/EliteYellow/EliteYellowEquipment.asset");
                _frenzied = _frenziedEquip.passiveBuffDef ? _frenziedEquip.passiveBuffDef : Buff("Frenzied", yellow, buffIcon);
                _frenziedEquip.passiveBuffDef = _frenzied;
                Describe(_frenziedEquip, "Frenzied", icon, model, "Aspecto Frenético", "Mais rápido. Use para se teleportar.",
                    "<style=cIsUtility>+2</style> de velocidade de movimento base e <style=cIsDamage>+50%</style> de velocidade de ataque. Ative para se <style=cIsUtility>teleportar 25 m</style> na direção do movimento.");
                _frenziedEquip.cooldown = 10f;
                AddT1("Frenzied", _frenziedEquip, yellow, "{0} Frenético", "RoR2/Base/Common/ColorRamps/texRampWarbanner2.png", t1, t1Honor, log);

                var orange = new Color32(255, 128, 0, 255);
                _empowering = Buff("Empowering", orange, buffIcon);
                var empoweringEquip = Aspect("Empowering", _empowering, icon, model, "Aspecto Fortalecedor", "Carregue um estandarte de guerra.",
                    "Aliados a até <style=cIsUtility>25 m</style> recebem o <style=cIsDamage>Warbanner</style>: <style=cIsDamage>+30%</style> de velocidade de ataque e de movimento.");
                AddT1("Empowering", empoweringEquip, orange, "{0} Fortalecedor", "RoR2/Base/Common/ColorRamps/texRampMagmaWorm.png", t1, t1Honor, log);

                var black = new Color32(26, 26, 26, 255);
                _blighted = Buff("Blighted", black, buffIcon);
                var blightedEquip = Aspect("Blighted", _blighted, icon, model, "Aspecto Arruinado", "Torne-se um aspecto da praga.",
                    "Receba <style=cIsDamage>2</style> afixos de elite aleatórios.");
                var blighted = Elite("Blighted", blightedEquip, black, "{0} Arruinado", "RoR2/DLC2/Items/DelayedDamage/texRampWarpedEcho.png", log);
                if (blighted)
                {
                    blighted.healthBoostCoefficient = BlightedHealth;
                    blighted.damageBoostCoefficient = BlightedDamage;
                    Place("Blighted", blighted, EliteAPI.GetEliteTierEnumerable(VanillaEliteTier.Tier2));
                }
                foreach (var key in new[] { "RoR2/Base/EliteFire/edFire.asset", "RoR2/Base/EliteIce/edIce.asset",
                             "RoR2/Base/EliteLightning/edLightning.asset", "RoR2/DLC1/EliteEarth/edEarth.asset" })
                {
                    var def = Load<EliteDef>(key);
                    if (def) BlightedPool.Add(def);
                }

                RegisterEcho(t2, log);
                log.LogInfo($"Elites: {Placements.Count} elite(s) registrados (Volátil, Frenético, Fortalecedor, Arruinado, Eco).");
            }
            catch (Exception e)
            {
                log.LogError($"Elites: falha ao registrar (o plugin segue sem eles): {e}");
            }
        }

        private static void RegisterEcho(EliteDef t2, ManualLogSource log)
        {
            try
            {
                var echo = Load<EliteDef>("RoR2/InDev/edEcho.asset");
                if (!echo) { log.LogWarning("Elites: edEcho indisponível; sem Eco."); return; }
                echo.healthBoostCoefficient = t2.healthBoostCoefficient;
                echo.damageBoostCoefficient = t2.damageBoostCoefficient;
                echo.modifierToken = "ROR2MEEEEE_ELITE_ECHO";
                LanguageAPI.Add(echo.modifierToken, "{0} Eco");
                var ramp = Load<Texture2D>("RoR2/Base/Common/ColorRamps/texRampShadowClone.png");
                if (ramp) EliteRamp.AddRamp(echo, ramp);
                if (echo.eliteEquipmentDef) Describe(echo.eliteEquipmentDef, "Echo", null, null, "Aspecto Eco", "Invoque ecos de si mesmo.",
                    "Invoca <style=cIsUtility>2 ecos</style> com <style=cIsHealth>10%</style> da vida, que atiram projéteis caçadores e somem quando você morre.");
                // O projétil dos ecos fica no grupo InDev, fora do catálogo do jogo: sem isto não chega aos clientes.
                var projectile = Load<GameObject>("RoR2/InDev/EchoHunterProjectile.prefab");
                if (projectile && !ContentAddition.AddProjectile(projectile)) log.LogWarning("Elites: ContentAddition recusou o projétil do Eco.");
                Place("Echo", echo, EliteAPI.GetEliteTierEnumerable(VanillaEliteTier.Tier2));
            }
            catch (Exception e)
            {
                log.LogWarning($"Elites: falha ao preparar o Eco (segue sem ele): {e.Message}");
            }
        }

        private static BuffDef Buff(string name, Color32 color, Sprite icon)
        {
            var buff = ScriptableObject.CreateInstance<BuffDef>();
            buff.name = "Ror2MeeeeeAffix" + name;
            buff.canStack = false;
            buff.isDebuff = false;
            buff.buffColor = color;
            buff.iconSprite = icon;
            if (!ContentAddition.AddBuffDef(buff)) throw new InvalidOperationException($"ContentAddition recusou o buff {buff.name}");
            return buff;
        }

        private static EquipmentDef Aspect(string name, BuffDef buff, Sprite icon, GameObject model, string displayName, string pickup, string desc)
        {
            var def = ScriptableObject.CreateInstance<EquipmentDef>();
            def.name = "Ror2MeeeeeAffix" + name;
            def.passiveBuffDef = buff;
            def.canDrop = false;
            def.enigmaCompatible = false;
            def.canBeRandomlyTriggered = false;
            def.appearsInSinglePlayer = true;
            def.appearsInMultiPlayer = true;
            def.colorIndex = ColorCatalog.ColorIndex.Equipment;
            def.cooldown = 0f;
            Describe(def, name, icon, model, displayName, pickup, desc);
            if (!ContentAddition.AddEquipmentDef(def)) throw new InvalidOperationException($"ContentAddition recusou o aspecto {def.name}");
            return def;
        }

        // Tokens próprios também no equipamento do jogo (Frenético, Eco), que não tem texto em português.
        private static void Describe(EquipmentDef def, string name, Sprite icon, GameObject model, string displayName, string pickup, string desc)
        {
            var token = "ROR2MEEEEE_AFFIX_" + name.ToUpperInvariant();
            def.nameToken = token + "_NAME";
            def.pickupToken = token + "_PICKUP";
            def.descriptionToken = token + "_DESC";
            def.loreToken = "";
            def.canDrop = false;
            def.dropOnDeathChance = DropChance;
            if (icon) def.pickupIconSprite = icon;
            if (model) def.pickupModelPrefab = model;
            LanguageAPI.Add(def.nameToken, displayName);
            LanguageAPI.Add(def.pickupToken, pickup);
            LanguageAPI.Add(def.descriptionToken, desc);
        }

        private static EliteDef Elite(string name, EquipmentDef equip, Color32 color, string modifier, string rampKey, ManualLogSource log)
        {
            var token = "ROR2MEEEEE_ELITE_" + name.ToUpperInvariant();
            LanguageAPI.Add(token, modifier);
            // Tiers vazios: o R2API.Elites só registra; quem põe e tira dos tiers é Sync (config do host).
            var custom = new CustomElite("EliteRor2Meeeee" + name, equip, color, token, Array.Empty<CombatDirector.EliteTierDef>());
            if (!EliteAPI.Add(custom)) { log.LogError($"Elites: EliteAPI recusou {name}."); return null; }
            var ramp = Load<Texture2D>(rampKey);
            if (ramp) EliteRamp.AddRamp(custom.EliteDef, ramp);
            if (equip.passiveBuffDef && !equip.passiveBuffDef.eliteDef) equip.passiveBuffDef.eliteDef = custom.EliteDef;
            return custom.EliteDef;
        }

        // T1: um EliteDef normal (estágios 1+) e um de Honra, com os stats do Flamejante de cada um.
        private static void AddT1(string name, EquipmentDef equip, Color32 color, string modifier, string rampKey,
            EliteDef template, EliteDef honorTemplate, ManualLogSource log)
        {
            var normal = Elite(name, equip, color, modifier, rampKey, log);
            if (!normal) return;
            normal.healthBoostCoefficient = template.healthBoostCoefficient;
            normal.damageBoostCoefficient = template.damageBoostCoefficient;
            Place(name, normal, EliteAPI.GetEliteTierEnumerable(VanillaEliteTier.BaseTier1));

            var honor = Elite(name + "Honor", equip, color, modifier, rampKey, log);
            if (!honor) return;
            honor.healthBoostCoefficient = honorTemplate.healthBoostCoefficient;
            honor.damageBoostCoefficient = honorTemplate.damageBoostCoefficient;
            Place(name, honor, EliteAPI.GetHonorEliteTierEnumerable(VanillaEliteTier.BaseTier1));
        }

        private static void Place(string key, EliteDef def, IEnumerable<CombatDirector.EliteTierDef> tiers)
        {
            var list = new List<CombatDirector.EliteTierDef>();
            foreach (var tier in tiers) if (tier != null) list.Add(tier);
            Placements.Add(new Placement { Key = key, Def = def, Tiers = list });
        }

        public ElitesFeature(Cfg cfg, ManualLogSource log) : base("Elites", cfg, log,
            "Elites novos sorteados pelo diretor: Volátil, Frenético e Fortalecedor (T1), Arruinado e Eco (T2, a partir do loop). Desligado: o diretor não os sorteia e os que já estão vivos perdem os efeitos extras.",
            enabledByDefault: true)
        {
            Bind("Volatile", "Volátil (T1): todo golpe explode e dispara mísseis a cada 1-5 s.");
            Bind("Frenzied", "Frenético (T1): mais velocidade de movimento e de ataque; se teleporta até perto do alvo a cada 10 s.");
            Bind("Empowering", "Fortalecedor (T1): aura de Warbanner de 25 m para os aliados.");
            Bind("Blighted", "Arruinado (T2): dois afixos de elite T1 aleatórios.");
            Bind("Echo", "Eco (T2): invoca 2 ecos com 10% da vida que atiram projéteis caçadores.");
        }

        private void Bind(string key, string description)
        {
            var entry = Cfg.Bind("Elites", key, true, description + " Só o host precisa; vale na hora (próximos spawns).");
            entry.SettingChanged += (_, __) => Sync(Enabled);
            _enabledByKey[key] = entry;
        }

        // Põe ou tira cada elite dos tiers do diretor (arrays lidos a cada sorteio).
        private void Sync(bool on)
        {
            foreach (var p in Placements)
            {
                var want = on && _enabledByKey.TryGetValue(p.Key, out var entry) && entry.Value;
                foreach (var tier in p.Tiers)
                {
                    var has = Array.IndexOf(tier.eliteTypes, p.Def) >= 0;
                    if (want == has) continue;
                    var types = new List<EliteDef>(tier.eliteTypes);
                    if (want) types.Add(p.Def); else types.Remove(p.Def);
                    tier.eliteTypes = types.ToArray();
                }
            }
        }

        public override void ResetRunState()
        {
            ClearState();
            _rng = null;
        }

        protected override void OnEnable()
        {
            Track("CharacterBody.OnBuffFirstStackGained", () => On.RoR2.CharacterBody.OnBuffFirstStackGained += OnBuffGained);
            Track("CharacterBody.OnBuffFinalStackLost", () => On.RoR2.CharacterBody.OnBuffFinalStackLost += OnBuffLost);
            Track("GlobalEventManager.OnHitAll", () => On.RoR2.GlobalEventManager.OnHitAll += OnHitAll);
            Track("EquipmentSlot.PerformEquipmentAction", () => On.RoR2.EquipmentSlot.PerformEquipmentAction += OnPerformEquipmentAction);
            Track("RoR2Application.onFixedUpdate", () => RoR2Application.onFixedUpdate += OnFixedUpdate);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += Forget);
            Sync(true);
        }

        protected override void OnDisable()
        {
            On.RoR2.CharacterBody.OnBuffFirstStackGained -= OnBuffGained;
            On.RoR2.CharacterBody.OnBuffFinalStackLost -= OnBuffLost;
            On.RoR2.GlobalEventManager.OnHitAll -= OnHitAll;
            On.RoR2.EquipmentSlot.PerformEquipmentAction -= OnPerformEquipmentAction;
            RoR2Application.onFixedUpdate -= OnFixedUpdate;
            CharacterBody.onBodyDestroyGlobal -= Forget;
            Sync(false);
            ClearState();
        }

        private void ClearState()
        {
            foreach (var ward in _wards.Values) if (ward) UnityEngine.Object.Destroy(ward);
            _wards.Clear();
            _missiles.Clear();
            _volatileByObject.Clear();
            _blinkers.Clear();
            _blightedAffixes.Clear();
        }

        private Xoroshiro128Plus Rng()
        {
            if (_rng == null) _rng = new Xoroshiro128Plus(Run.instance ? Run.instance.seed ^ 0x454C495445530000UL : 0UL); // "ELITES"
            return _rng;
        }

        private static float Now => Run.instance ? Run.instance.fixedTime : 0f;

        // Buffs chegam em toda máquina; os efeitos são só do host (mísseis, blink, ward e afixos são de rede).
        private void OnBuffGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (!NetworkServer.active || !buffDef || !self) return;
            try
            {
                if (buffDef == _volatile)
                {
                    _volatileByObject[self.gameObject] = self;
                    _missiles.Add(new Timed { Body = self, Next = Now + NextMissileDelay() });
                }
                else if (buffDef == _frenzied)
                {
                    if (!self.isPlayerControlled) _blinkers.Add(new Timed { Body = self, Next = Now + BlinkInterval });
                }
                else if (buffDef == _empowering) SpawnWard(self);
                else if (buffDef == _blighted) AddBlightedAffixes(self);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBuffLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (!NetworkServer.active || !buffDef || !self) return;
            try
            {
                if (buffDef == _volatile) ForgetVolatile(self);
                else if (buffDef == _frenzied) RemoveTimed(_blinkers, self);
                else if (buffDef == _empowering) DestroyWard(self);
                else if (buffDef == _blighted) RemoveBlightedAffixes(self);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void Forget(CharacterBody body)
        {
            if (!body) return;
            ForgetVolatile(body);
            RemoveTimed(_blinkers, body);
            DestroyWard(body);
            _blightedAffixes.Remove(body);
        }

        private void ForgetVolatile(CharacterBody body)
        {
            if (_volatileByObject.Count == 0) return;
            _volatileByObject.Remove(body.gameObject);
            RemoveTimed(_missiles, body);
        }

        private static void RemoveTimed(List<Timed> list, CharacterBody body)
        {
            for (var i = list.Count - 1; i >= 0; i--) if (list[i].Body == body) list.RemoveAt(i);
        }

        private float NextMissileDelay() => Rng().RangeInt(1, 6); // 1 a 5 s, como no original

        // Por tick, só no host e só com elite vivo: varre duas listas pequenas.
        private void OnFixedUpdate()
        {
            if ((_missiles.Count == 0 && _blinkers.Count == 0) || !NetworkServer.active) return;
            try
            {
                var now = Now;
                for (var i = 0; i < _missiles.Count; i++)
                {
                    var t = _missiles[i];
                    if (now < t.Next) continue;
                    t.Next = now + NextMissileDelay();
                    var body = t.Body;
                    if (!body || !body.healthComponent || !body.healthComponent.alive || !_missile) continue;
                    // Direção fixa e sem crítico: a sobrecarga sem direção e o Util.CheckRoll sorteiam fora do RNG do mod.
                    MissileUtils.FireMissile(body.corePosition, body, default, null, body.damage * 0.4f, false, _missile, DamageColorIndex.Item, Vector3.up, 200f, false);
                }
                for (var i = 0; i < _blinkers.Count; i++)
                {
                    var t = _blinkers[i];
                    if (now < t.Next) continue;
                    t.Next = now + BlinkInterval;
                    if (t.Body) BlinkTowardEnemy(t.Body);
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Volátil: a explosão do Brilliant Behemoth com 1 cópia. procCoefficient 0 na explosão: não reexplode.
        private void OnHitAll(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (_volatileByObject.Count == 0 || !NetworkServer.active) return;
            try
            {
                if (damageInfo.procCoefficient == 0f || damageInfo.rejected || !damageInfo.attacker) return;
                if (!_volatileByObject.TryGetValue(damageInfo.attacker, out var body) || !body) return;
                var radius = 4f * damageInfo.procCoefficient;
                if (_explosion)
                    EffectManager.SpawnEffect(_explosion, new EffectData
                    {
                        origin = damageInfo.position,
                        scale = radius,
                        rotation = Util.QuaternionSafeLookRotation(damageInfo.force)
                    }, transmit: true);
                new BlastAttack
                {
                    position = damageInfo.position,
                    baseDamage = Util.OnHitProcDamage(damageInfo.damage, body.damage, 0.6f),
                    baseForce = 0f,
                    radius = radius,
                    attacker = damageInfo.attacker,
                    inflictor = null,
                    teamIndex = TeamComponent.GetObjectTeam(damageInfo.attacker),
                    crit = damageInfo.crit,
                    procChainMask = damageInfo.procChainMask,
                    procCoefficient = 0f,
                    damageColorIndex = DamageColorIndex.Item,
                    falloffModel = BlastAttack.FalloffModel.None,
                    damageType = damageInfo.damageType
                }.Fire();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Frenético usado por jogador: blink de 25 m na direção do movimento (ou da mira). PerformEquipmentAction é [Server].
        private bool OnPerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if (!_frenziedEquip || equipmentDef != _frenziedEquip) return orig(self, equipmentDef);
            try
            {
                var body = self.characterBody;
                if (!NetworkServer.active || !body || !body.inputBank) return false;
                var dir = body.inputBank.moveVector;
                if (dir.sqrMagnitude < 0.01f) dir = body.inputBank.aimDirection;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) dir = body.transform.forward;
                return Blink(body, body.footPosition + dir.normalized * PlayerBlinkDistance);
            }
            catch (Exception e)
            {
                Fail(e);
                return false; // sem cooldown se falhou
            }
        }

        // IA: até 15 m do alvo atual, se ele estiver a até 50 m (o alvo vem da IA; sem busca por tick).
        private void BlinkTowardEnemy(CharacterBody body)
        {
            var master = body.master;
            var ais = master ? master.AiComponents : null;
            var enemy = ais != null && ais.Length > 0 && ais[0] && ais[0].currentEnemy != null ? ais[0].currentEnemy.characterBody : null;
            if (!enemy || (enemy.corePosition - body.corePosition).sqrMagnitude > BlinkRange * BlinkRange) return;
            var rng = Rng();
            var angle = rng.nextNormalizedFloat * 2f * Mathf.PI;
            var r = BlinkSpread * Mathf.Sqrt(rng.nextNormalizedFloat);
            Blink(body, enemy.footPosition + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
        }

        // No nó do mapa mais perto do destino (chão ou ar, conforme o corpo); false se não há nó.
        private static bool Blink(CharacterBody body, Vector3 target)
        {
            var scene = SceneInfo.instance;
            var nodes = scene ? (body.isFlying ? scene.airNodes : scene.groundNodes) : null;
            if (!nodes) return false;
            var node = nodes.FindClosestNode(target, body.hullClassification);
            if (node == RoR2.Navigation.NodeGraph.NodeIndex.invalid || !nodes.GetNodePosition(node, out var dest)) return false;
            if (_blink) EffectManager.SpawnEffect(_blink, new EffectData { origin = body.corePosition, scale = body.radius }, transmit: true);
            TeleportHelper.TeleportBody(body, dest);
            if (_blink) EffectManager.SpawnEffect(_blink, new EffectData { origin = dest, scale = body.radius }, transmit: true);
            return true;
        }

        // Ward da Celestine (já é objeto de rede do jogo) com o Warbanner no lugar da invisibilidade. O buff só é aplicado
        // no host; nos clientes o anel continua com a cara do ward da Celestine.
        private void SpawnWard(CharacterBody body)
        {
            if (!_ward || _wards.ContainsKey(body)) return;
            var ward = UnityEngine.Object.Instantiate(_ward);
            ward.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
            var buffWard = ward.GetComponent<BuffWard>();
            buffWard.buffDef = RoR2Content.Buffs.Warbanner;
            buffWard.Networkradius = EmpoweringRadius + body.radius;
            ward.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
            _wards[body] = ward;
        }

        private void DestroyWard(CharacterBody body)
        {
            if (!_wards.TryGetValue(body, out var ward)) return;
            if (ward) UnityEngine.Object.Destroy(ward);
            _wards.Remove(body);
        }

        private void AddBlightedAffixes(CharacterBody body)
        {
            if (_blightedAffixes.ContainsKey(body)) return;
            _pool.Clear();
            foreach (var def in BlightedPool)
            {
                var buff = def.eliteEquipmentDef ? def.eliteEquipmentDef.passiveBuffDef : null;
                if (buff && def.IsAvailable() && !body.HasBuff(buff)) _pool.Add(buff);
            }
            var rng = Rng();
            var count = _pool.Count;
            var (first, second) = EliteRules.PickTwoDistinct(count, count > 0 ? rng.RangeInt(0, count) : 0, count > 1 ? rng.RangeInt(0, count - 1) : 0);
            var picked = new BuffDef[2];
            if (first >= 0) { picked[0] = _pool[first]; body.AddBuff(picked[0]); }
            if (second >= 0) { picked[1] = _pool[second]; body.AddBuff(picked[1]); }
            _blightedAffixes[body] = picked;
        }

        private void RemoveBlightedAffixes(CharacterBody body)
        {
            if (!_blightedAffixes.TryGetValue(body, out var picked)) return;
            foreach (var buff in picked) if (buff && body.HasBuff(buff)) body.RemoveBuff(buff);
            _blightedAffixes.Remove(body);
        }
    }
}
