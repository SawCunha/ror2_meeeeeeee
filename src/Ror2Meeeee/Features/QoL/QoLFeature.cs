using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using MonoMod.Cil;
using RoR2;
using RoR2.Navigation;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Qualidade de vida, ideia do WolfoQoL_Gameplay 1.1.0 (Wolfo), reescrita e conferida no jogo 1.5.0:
    //  - Itens fora do mapa: a cada 1 s o host olha os pickups (orbe em voo, item no chão, Command Cube/Void Potential/
    //    Aurelionite Fragment) e leva quem estiver fora dos limites (MapZone OutOfBounds) ao nó de chão mais próximo.
    //    O Wolfo trocava camadas de colisão por GUIDs Addressables; aqui a varredura não depende da matriz de colisão.
    //  - Drones: o DeathState do drone só cria o drone quebrado (para consertar) se tocar o chão em até 2 s; caindo
    //    no buraco ou atravessando o chão, o drone sumia. Fora dos limites ou no fim do prazo, o drone quebrado nasce no
    //    nó de chão mais próximo. OnImpactServer do jogo já ignora fantasmas, Dio's e quem já criou o drone quebrado.
    //  - Lava: aliados que não são jogadores (drones, torretas) levavam 10% da vida por toque, como o jogador; agora 2%,
    //    como monstros comuns. IL em InflictLavaDamage: o primeiro teamIndex lido vira Monster para esses corpos.
    //  - Habilidades sem dano (bolha do Engi, Sonic Boom do REX, gancho do Loader, mira do Railgunner) não colocam o
    //    jogador em combate (SkillDef.isCombatSkill = false). As chaves Addressables do Wolfo não existem no 1.5.0:
    //    a busca é pelo nome do asset no SkillCatalog.
    // Itens/drones/lava só rodam no host; a mudança das habilidades vale em todas as máquinas (todos usam o mod).
    public sealed class QoLFeature : Feature
    {
        private const float ScanInterval = 1f;
        private static readonly string[] NonCombatSkills =
            { "EngiBodyPlaceBubbleShield", "TreebotBodySonicBoom", "FireHook", "RailgunnerBodyScopeLight", "RailgunnerBodyScopeHeavy" };

        private static readonly FieldInfo SpawnedRepairField =
            typeof(EntityStates.Drone.DeathState).GetField("spawnedRepairObject", BindingFlags.Instance | BindingFlags.NonPublic);
        // fixedAge do estado é protegido; delegate aberto, sem boxing por tick.
        private static readonly Func<EntityStates.EntityState, float> FixedAge = (Func<EntityStates.EntityState, float>)Delegate.CreateDelegate(
            typeof(Func<EntityStates.EntityState, float>),
            typeof(EntityStates.EntityState).GetProperty("fixedAge", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));

        private readonly ConfigEntry<bool> _items, _drones, _lava, _skills;
        private readonly List<SkillDef> _changedSkills = new List<SkillDef>();
        private float _acc;
        private int _itemsMoved, _dronesSaved;

        public QoLFeature(Cfg cfg, ManualLogSource log) : base("QoL", cfg, log,
            "Quality of life (idea from WolfoQoL_Gameplay): items and dead drones that fall off the map come back, ally lava damage like monsters' (2%), and non-damaging skills don't put you in combat.", "Qualidade de vida (ideia do WolfoQoL_Gameplay): itens e drones mortos que caem do mapa voltam, lava nos aliados igual à dos monstros (2%) e habilidades sem dano não colocam em combate.",
            enabledByDefault: true)
        {
            _items = cfg.Bind("QoL", "ItemsReturn", true,
                "Items that fall off the map come back to the nearest ground.", "Itens que caem do mapa voltam ao chão mais próximo.");
            _drones = cfg.Bind("QoL", "DronesReturn", true,
                "Dead drones that fall off the map or through the ground leave the broken drone on the nearest ground.", "Drones mortos que caem do mapa ou atravessam o chão deixam o drone quebrado no chão mais próximo.");
            _lava = cfg.Bind("QoL", "AllyLava", true,
                "Lava deals 2% of health to non-player allies (drones, turrets), like common monsters, instead of 10%.", "Lava tira 2% da vida dos aliados que não são jogadores (drones, torretas), como nos monstros comuns, e não 10%.");
            _skills = cfg.Bind("QoL", "NonCombatSkills", true,
                "Engi bubble, REX Sonic Boom, Loader hook and Railgunner scope don't put you in combat. Takes effect on game start.", "Bolha do Engi, Sonic Boom do REX, gancho do Loader e mira do Railgunner não colocam em combate. Vale ao iniciar o jogo.");
        }

        public override void ResetRunState()
        {
            if (_itemsMoved + _dronesSaved > 0)
                Log.LogInfo($"[QoL] run: {_itemsMoved} item(ns) trazido(s) de volta, {_dronesSaved} drone(s) quebrado(s) salvo(s).");
            _itemsMoved = _dronesSaved = 0;
            _acc = 0f;
        }

        protected override void OnEnable()
        {
            RoR2Application.onFixedUpdate += OnFixedUpdate;
            Track("Drone.DeathState.FixedUpdate", () => On.EntityStates.Drone.DeathState.FixedUpdate += OnDroneDeathFixedUpdate);
            Track("CharacterBody.InflictLavaDamage (IL)", () => IL.RoR2.CharacterBody.InflictLavaDamage += ILAllyLava);
            if (_skills.Value)
            {
                if (SkillCatalog.skillCount > 0) SetNonCombatSkills();
                else RoR2Application.onLoad += SetNonCombatSkills;
            }
        }

        protected override void OnDisable()
        {
            RoR2Application.onFixedUpdate -= OnFixedUpdate;
            On.EntityStates.Drone.DeathState.FixedUpdate -= OnDroneDeathFixedUpdate;
            IL.RoR2.CharacterBody.InflictLavaDamage -= ILAllyLava;
            RoR2Application.onLoad -= SetNonCombatSkills;
            foreach (var s in _changedSkills) if (s) s.isCombatSkill = true;
            _changedSkills.Clear();
        }

        private void SetNonCombatSkills()
        {
            try
            {
                foreach (var s in SkillCatalog.allSkillDefs)
                {
                    if (!s || !s.isCombatSkill || Array.IndexOf(NonCombatSkills, ((ScriptableObject)s).name) < 0) continue;
                    s.isCombatSkill = false;
                    _changedSkills.Add(s);
                }
                Log.LogInfo($"[QoL] {_changedSkills.Count}/{NonCombatSkills.Length} habilidade(s) sem dano fora de combate.");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // OutOfBounds com TriggerExit: sair do volume é estar fora; com TriggerEnter: entrar é estar fora.
        private static bool IsOutOfBounds(Vector3 p)
        {
            foreach (var zone in InstanceTracker.GetInstancesList<MapZone>())
            {
                if (zone.zoneType != MapZone.ZoneType.OutOfBounds) continue;
                var inside = zone.IsPointInsideMapZone(p);
                if (zone.triggerType == MapZone.TriggerType.TriggerExit ? !inside : inside) return true;
            }
            return false;
        }

        private static bool NearestGround(Vector3 p, out Vector3 ground)
        {
            ground = default;
            var graph = SceneInfo.instance ? SceneInfo.instance.groundNodes : null;
            if (!graph) return false;
            var node = graph.FindClosestNode(p, HullClassification.Human);
            return node != NodeGraph.NodeIndex.invalid && graph.GetNodePosition(node, out ground);
        }

        private void OnFixedUpdate()
        {
            try
            {
                if (!_items.Value || !NetworkServer.active || !Run.instance) return;
                _acc += Time.fixedDeltaTime;
                if (_acc < ScanInterval) return;
                _acc = 0f;
                ReturnItems(InstanceTracker.GetInstancesList<PickupDropletController>());
                ReturnItems(InstanceTracker.GetInstancesList<GenericPickupController>());
                ReturnItems(InstanceTracker.GetInstancesList<PickupPickerController>());
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void ReturnItems<T>(List<T> list) where T : MonoBehaviour
        {
            // De trás para frente: o teleporte não remove da lista, mas um Destroy no meio do caminho poderia.
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (!item) continue;
                var p = item.transform.position;
                if (!IsOutOfBounds(p) || !NearestGround(p, out var ground)) continue;
                var go = item.gameObject;
                var dest = ground + Vector3.up * 2f;
                if (IsOutOfBounds(dest)) continue; // nó também fora: não teleportar a cada segundo à toa
                TeleportHelper.TeleportGameObject(go, dest);
                var rb = go.GetComponent<Rigidbody>();
                if (rb) rb.velocity = Vector3.zero;
                var effect = Run.instance.GetTeleportEffectPrefab(go);
                if (effect) EffectManager.SimpleEffect(effect, dest, Quaternion.identity, true);
                _itemsMoved++;
                Log.LogInfo($"[QoL] {go.name} caiu do mapa em {p}; trazido para {dest}.");
            }
        }

        private void OnDroneDeathFixedUpdate(On.EntityStates.Drone.DeathState.orig_FixedUpdate orig, EntityStates.Drone.DeathState self)
        {
            orig(self);
            try
            {
                if (!_drones.Value || !NetworkServer.active) return;
                var body = self.outer ? self.outer.GetComponent<CharacterBody>() : null; // characterBody do estado é protegido
                if (!body || !body.teamComponent || body.teamComponent.teamIndex != TeamIndex.Player || body.isPlayerControlled) return;
                var p = body.corePosition;
                // orig já explodiu no prazo (sem chão = sem drone quebrado) ou o drone está caindo no vazio.
                if (FixedAge(self) <= self.deathDuration && !IsOutOfBounds(p)) return;
                if (!NearestGround(p, out var ground)) return;
                // O jogo ignora se já criou o drone quebrado (ou se é fantasma/tem Dio's); conta só quando muda algo.
                var before = SpawnedRepairField != null && (bool)SpawnedRepairField.GetValue(self);
                self.OnImpactServer(ground);
                self.Explode();
                if (!before && SpawnedRepairField != null && (bool)SpawnedRepairField.GetValue(self))
                {
                    _dronesSaved++;
                    Log.LogInfo($"[QoL] {body.name} morreu sem chão em {p}; drone quebrado em {ground}.");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // `teamComponent.teamIndex` (primeira leitura, comparada com Monster) vira Monster para aliado não jogador.
        // Campeões e chefes vêm antes, no ramo de 1%, e não mudam. Corpo mudou = loga e não altera nada.
        private void ILAllyLava(ILContext il)
        {
            if (!_lava.Value) return;
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<TeamComponent>("get_teamIndex")))
            {
                Log.LogError("[QoL] lava: leitura de teamIndex em InflictLavaDamage não encontrada; IL hook não aplicado.");
                return;
            }
            c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            c.EmitDelegate<Func<TeamIndex, CharacterBody, TeamIndex>>((team, body) =>
                team == TeamIndex.Player && !body.isPlayerControlled ? TeamIndex.Monster : team);
        }
    }
}
