using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using RoR2;
using Ror2Meeeee.Core.Helluva;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618 // pickupIconSprite/pickupModelPrefab: campos "left for modders' use" no 1.5.0

namespace Ror2Meeeee
{
    // Equipamento lunar "Grimório do Stolas": teleporta quem usa para o primeiro aliado vivo encontrado
    // (ordem da lista de jogadores do jogo); sozinho, para o teleportador do estágio.
    // O preço do contrato: cada uso dá crédito de spawn ao diretor (uma onda de inimigos a caminho).
    // Só no host (PerformEquipmentAction é [Server]); o teleporte é propagado pelo próprio jogo (TeleportMessage).
    public sealed class GrimoireFeature : Feature
    {
        public const string EquipName = "Ror2MeeeeeStolasGrimoire";
        public const string Token = "ROR2MEEEEE_EQUIP_GRIMOIRE";

        private static EquipmentDef _def;

        private readonly ConfigEntry<float> _waveCredit;
        private float _waveCreditValue;
        private readonly ConfigEntry<float> _maxPerUse;
        private readonly ConfigEntry<int> _maxMonsters;
        private float _maxPerUseValue;
        private int _maxMonstersValue;
        private Xoroshiro128Plus _rng; // do mod: não consome RNG da run

        // Chamar no Awake do plugin (antes do EquipmentCatalog). Falha não derruba o plugin.
        public static void Register(ManualLogSource log)
        {
            try
            {
                var icon = TitheArtifact.LoadIcon("grimorio.png", new Color32(70, 120, 220, 255));
                var model = ModItem.Model("RoR2/Base/BonusGoldPackOnKill/PickupTome.prefab", "Grimório", log);

                var equip = new CustomEquipment(EquipName, Token + "_NAME", Token + "_DESC", Token + "_LORE", Token + "_PICKUP",
                    icon, model, cooldown: 60f, canDrop: true, enigmaCompatible: false, isBoss: false, isLunar: true,
                    passiveBuffDef: null, unlockableDef: null, colorIndex: ColorCatalog.ColorIndex.LunarItem,
                    itemDisplayRules: Array.Empty<ItemDisplayRule>());
                LanguageAPI.Add(Token + "_NAME", "Grimório do Stolas");
                LanguageAPI.Add(Token + "_PICKUP", "Abre um portal até um aliado. <style=cDeath>Todo contrato tem um preço.</style>");
                LanguageAPI.Add(Token + "_DESC",
                    "Teleporta você para perto do <style=cIsUtility>primeiro aliado vivo</style> que encontrar; sem aliados, para o " +
                    "<style=cIsUtility>teleportador</style> do estágio. " +
                    "<style=cDeath>Cada uso dá crédito ao diretor: uma onda de inimigos está a caminho.</style>");
                LanguageAPI.Add(Token + "_LORE", "Emprestado. Sem pedir. De novo.");

                if (!ItemAPI.Add(equip)) { log.LogError("Grimório: ItemAPI.Add recusou o equipamento."); return; }
                _def = equip.EquipmentDef;
                log.LogInfo($"Grimório: equipamento '{EquipName}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Grimório: falha ao registrar (o plugin segue sem ele): {e}");
                _def = null;
            }
        }

        public GrimoireFeature(Cfg cfg, ManualLogSource log) : base("Grimoire", cfg, log,
            "Equipamento lunar Grimório do Stolas: teleporta para perto do primeiro aliado vivo (sozinho: para o teleportador do estágio), mas cada uso dá crédito ao diretor (onda de inimigos). Desligado: o equipamento existe mas não faz nada.",
            enabledByDefault: true)
        {
            _waveCredit = cfg.Bind("Grimoire", "WaveCredit", 150f,
                "Crédito de spawn dado ao diretor a cada uso, multiplicado pela dificuldade atual (150 = uma onda média). 0 = sem preço.");
            _waveCreditValue = _waveCredit.Value;
            _waveCredit.SettingChanged += (_, __) => _waveCreditValue = _waveCredit.Value;
            _maxPerUse = cfg.Bind("Grimoire", "MaxCreditPerUse", 600f,
                "Teto de crédito por uso (a dificuldade multiplica o WaveCredit e, no fim da run, passava de 1400 por uso). 0 = sem teto.");
            _maxPerUseValue = _maxPerUse.Value;
            _maxPerUse.SettingChanged += (_, __) => _maxPerUseValue = _maxPerUse.Value;
            _maxMonsters = cfg.Bind("Grimoire", "MaxMonsters", 80,
                "Com tantos monstros vivos (ou mais), o uso ainda teleporta mas não dá crédito (já há inimigos demais). 0 = sem trava.");
            _maxMonstersValue = _maxMonsters.Value;
            _maxMonsters.SettingChanged += (_, __) => _maxMonstersValue = _maxMonsters.Value;
        }

        public override void ResetRunState() { _rng = null; }

        protected override void OnEnable()
        {
            Track("EquipmentSlot.PerformEquipmentAction", () => On.RoR2.EquipmentSlot.PerformEquipmentAction += OnPerformEquipmentAction);
        }

        protected override void OnDisable()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction -= OnPerformEquipmentAction;
        }

        private bool OnPerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if (!_def || equipmentDef != _def) return orig(self, equipmentDef);
            try
            {
                return Activate(self);
            }
            catch (Exception e)
            {
                Fail(e);
                return false; // sem cooldown se falhou
            }
        }

        private const float MaxNodeDistance = 10f;

        // 2 m atrás do aliado, no chão (raio para baixo); sem chão, ao lado dele, 1 m acima.
        private static Vector3 BehindAlly(CharacterBody ally)
        {
            var feet = ally.footPosition;
            var start = feet - ally.transform.forward * 2f + Vector3.up * 1.5f;
            return Physics.Raycast(start, Vector3.down, out var hit, 6f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * 0.2f
                : feet + Vector3.up;
        }

        // Primeiro jogador vivo que não seja quem usou (operando drone à distância = morto, como na lápide).
        private static CharacterBody FirstAlly(CharacterBody self)
        {
            var list = PlayerCharacterMasterController.instances;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i] ? list[i].master : null;
                var b = m ? m.GetBody() : null;
                if (!b || b == self || b.isRemoteOp || !b.healthComponent || !b.healthComponent.alive) continue;
                return b;
            }
            return null;
        }

        // false = não gasta o uso (sozinho e sem teleportador neste estágio, ex.: Bazar, Lua).
        private bool Activate(EquipmentSlot slot)
        {
            if (!NetworkServer.active) return false;
            var body = slot.characterBody;
            var run = Run.instance;
            if (!body || !run) return false;
            var ally = FirstAlly(body);
            var tp = TeleporterInteraction.instance;
            if (!ally && !tp) return false;

            if (_rng == null) _rng = new Xoroshiro128Plus(run.seed ^ 0x53544F4C41530000UL); // "STOLAS"
            var target = ally ? ally.footPosition : tp.transform.position;
            // FindSafeTeleportDestination procura o nó livre mais próximo e, se não acha (nó ocupado pelo próprio aliado,
            // grafo pequeno como no Bazar), cai num ponto ALEATÓRIO do mapa. Com aliado: só aceita o nó se estiver perto.
            var node = TeleportHelper.FindSafeTeleportDestination(target, body, _rng);
            Vector3 dest;
            if (!ally) dest = node ?? target + Vector3.up * 2f;
            else dest = node.HasValue && (node.Value - target).sqrMagnitude <= MaxNodeDistance * MaxNodeDistance ? node.Value : BehindAlly(ally);
            // Nome do jogador, sem títulos de elite ("SawCunha", não "SawCunha Flamejante de Ouro").
            var where = ally ? (ally.master ? Util.GetBestMasterName(ally.master) : Util.GetBestBodyName(ally.gameObject)) : "o teleportador";

            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TeleportOutBoom"),
                new EffectData { origin = body.corePosition, rotation = Quaternion.identity }, transmit: true);
            TeleportHelper.TeleportBody(body, dest);

            var alive = TeamComponent.GetTeamMembers(TeamIndex.Monster).Count;
            var full = HelluvaRules.GrimoireCredit(_waveCreditValue, run.difficultyCoefficient);
            var credit = HelluvaRules.GrimoireCreditGuarded(_waveCreditValue, run.difficultyCoefficient, _maxPerUseValue, alive, _maxMonstersValue);
            Diag.GrimoireUses++;
            if (credit < full) Diag.GrimoireTrimmed += full - credit;
            var n = 0;
            if (credit > 0f)
            {
                var list = CombatDirector.instancesList;
                for (var i = 0; i < list.Count; i++)
                {
                    var d = list[i];
                    if (!d || !d.enabled || !Directors.IsMainMonster(d)) continue; // reflexão por diretor: só a cada uso do equipamento
                    d.monsterCredit += credit;
                    n++;
                }
            }
            var who = body.master ? Util.GetBestMasterName(body.master) : Util.GetBestBodyName(body.gameObject);
            // Sem diretor de combate ativo (Bazar, fim de estágio...) não há onda: a mensagem não promete o que não acontece.
            var price = n > 0 ? $"O contrato foi cobrado: {credit:F0} de crédito para o diretor." : "Sem diretor ativo: nenhuma onda desta vez.";
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#7aa2f7>Stolas:</color> {who} usou o grimório e foi até {where}. {price}"
            });
            Log.LogInfo($"[Grimoire] {who} teleportado até {where} (chegou a {Vector3.Distance(dest, target):F1} m do destino); +{credit:F0} crédito em {n} diretor(es) (cheio {full:F0}, monstros vivos {alive})");
            return true;
        }
    }
}
