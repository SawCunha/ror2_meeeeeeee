using System;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Correções de bugs do jogo 1.5.0 que geram NullReferenceException em série (centenas por run, com pilha no log,
    // o que custa FPS). Todos vistos numa run de 3 jogadores (2026-10-17):
    //  - Runic Lens: DetonateRunicLensMeteor estoura se o atacante morreu antes do meteoro cair, antes de FixedUpdate
    //    zerar runicLensMeteorReady: repete a cada tick. No tick da detonação, sem atacante, o meteoro é cancelado.
    //  - Unstable Transmitter, Update: corpo sem inventário (master destruído) estoura a cada frame. Update pulado.
    //  - Unstable Transmitter, orbe: dono já destruído estoura em OnTriggerEnter (cachedOwnerInfo.characterBody nulo);
    //    inimigo sem modelTransform estoura em TeleportAwayFrom (e o DoWarp perdia sangramento/atordoamento/debuff).
    //    Sem dono, o orbe ignora o contato; sem modelo, o inimigo não é teleportado e o resto do DoWarp segue.
    //  - War Bonds (BarrageOnBoss), OnDisable: torreta sem master estoura só na última linha (desinscrição no master,
    //    que já não existe); o resto já rodou. orig roda e a exceção é engolida só nesse caso.
    //  - EquipmentSlot: corpo sem inventário com requestEquipmentFire estoura a cada tick. O pedido é descartado
    //    antes de orig.
    //  - Artefato do Enxame (Swarms): CombatDirector.Spawn(SpawnCard, ..., bool) preenche minDistance/maxDistance mas
    //    não minDistanceSwarms/maxDistanceSwarms (ficam 0); as cópias do Enxame usam esses campos e nasciam em cima do
    //    alvo ou falhavam. Em Spawn(SpawnArgs), por onde todo spawn passa, distâncias de Enxame zeradas recebem as
    //    normais, como o jogo já faz em SetupDirectorPlacementRule. Mesmo bug corrigido pelo SwarmsFix (Thrayonlosa).
    //  - Elite Dourado (Aurelionite) em IA (drone, aliado, monstro) batendo em quem não é jogador: o jogo soltava pepitas
    //    de ouro a cada golpe, sem o cooldown de 0,5 s nem a chance que o jogador tem, e a partida travava. IL: esse caso
    //    passa pelo mesmo trecho do jogador (cooldown + chance, 1 pepita). IA roubando ouro de jogador não muda.
    //    Mesmo bug corrigido pelo GildedChunkSpamFix (LordVGames); não instale os dois.
    // Exceção à regra de sempre chamar orig: Update do Transmitter, OnTriggerEnter e TeleportAwayFrom só pulam orig
    // quando ele lançaria a exceção com certeza (mesmo padrão do GhostBladeFeature).
    public sealed class GameFixesFeature : Feature
    {
        private long _meteors, _transmitterUpdates, _orbContacts, _warps, _warBonds, _equipment, _swarms;

        public GameFixesFeature(Cfg cfg, ManualLogSource log) : base("GameFixes", cfg, log,
            "Corrige erros do jogo que se repetiam centenas de vezes por partida e custavam FPS: Runic Lens, Unstable Transmitter, War Bonds e equipamento de corpos sem inventário. Também conserta o artefato do Enxame (cópias nasciam em cima do jogador ou não nasciam) e drones/monstros Dourados que travavam a partida de tanta pepita de ouro. Não muda o jogo além disso.",
            enabledByDefault: true)
        {
        }

        public override void ResetRunState()
        {
            var total = _meteors + _transmitterUpdates + _orbContacts + _warps + _warBonds + _equipment + _swarms;
            if (total > 0)
                Log.LogInfo($"[GameFixes] run: Runic Lens {_meteors}, Transmitter Update {_transmitterUpdates}, orbe sem dono {_orbContacts}, "
                    + $"teleporte sem modelo {_warps}, War Bonds {_warBonds}, equipamento {_equipment} erro(s) evitados; Enxame: {_swarms} spawn(s) corrigidos.");
            _meteors = _transmitterUpdates = _orbContacts = _warps = _warBonds = _equipment = _swarms = 0;
        }

        protected override void OnEnable()
        {
            Track("MeteorAttackOnHighDamageBodyBehavior.FixedUpdate", () => On.RoR2.MeteorAttackOnHighDamageBodyBehavior.FixedUpdate += OnMeteorFixedUpdate);
            Track("TeleportOnLowHealthBehavior.Update", () => On.RoR2.TeleportOnLowHealthBehavior.Update += OnTransmitterUpdate);
            Track("TeleportOnLowHealthAuraController.OnTriggerEnter", () => On.TeleportOnLowHealthAuraController.OnTriggerEnter += OnOrbTriggerEnter);
            Track("CharacterBody.TeleportAwayFrom", () => On.RoR2.CharacterBody.TeleportAwayFrom += OnTeleportAwayFrom);
            Track("BarrageOnBossBehaviour.OnDisable", () => On.RoR2.BarrageOnBossBehaviour.OnDisable += OnWarBondsDisable);
            Track("EquipmentSlot.FixedUpdate", () => On.RoR2.EquipmentSlot.FixedUpdate += OnEquipmentFixedUpdate);
            Track("CombatDirector.Spawn(SpawnArgs)", () => On.RoR2.CombatDirector.Spawn_SpawnArgs += OnDirectorSpawn);
            Track("AffixAurelioniteBehavior.OnServerDamageDealt (IL)", () => IL.RoR2.AffixAurelioniteBehavior.OnServerDamageDealt += ILGildedNuggetCooldown);
        }

        protected override void OnDisable()
        {
            On.RoR2.MeteorAttackOnHighDamageBodyBehavior.FixedUpdate -= OnMeteorFixedUpdate;
            On.RoR2.TeleportOnLowHealthBehavior.Update -= OnTransmitterUpdate;
            On.TeleportOnLowHealthAuraController.OnTriggerEnter -= OnOrbTriggerEnter;
            On.RoR2.CharacterBody.TeleportAwayFrom -= OnTeleportAwayFrom;
            On.RoR2.BarrageOnBossBehaviour.OnDisable -= OnWarBondsDisable;
            On.RoR2.EquipmentSlot.FixedUpdate -= OnEquipmentFixedUpdate;
            On.RoR2.CombatDirector.Spawn_SpawnArgs -= OnDirectorSpawn;
            IL.RoR2.AffixAurelioniteBehavior.OnServerDamageDealt -= ILGildedNuggetCooldown;
        }

        // `else if (isPlayer) { cooldown + chance; 1 pepita }` vira `else if (true)`: o primeiro `if` (IA roubando de
        // jogador) já desviou antes, então só IA contra não jogador muda. Corpo mudou = loga e não altera nada.
        private void ILGildedNuggetCooldown(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<AffixAurelioniteBehavior>("isPlayer"),
                    x => x.MatchBrfalse(out _)))
            {
                Log.LogError("[GameFixes] Dourado: trecho isPlayer do OnServerDamageDealt não encontrado; IL hook não aplicado.");
                return;
            }
            c.Index--; // antes do brfalse
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_1);
        }

        private void OnMeteorFixedUpdate(On.RoR2.MeteorAttackOnHighDamageBodyBehavior.orig_FixedUpdate orig, MeteorAttackOnHighDamageBodyBehavior self)
        {
            try
            {
                var body = self.body;
                // GetComponent só no tick da detonação (uma vez por meteoro), não em todo tick.
                if (NetworkServer.active && body && body.runicLensMeteorReady && Run.instance
                    && Run.instance.time >= body.runicLensImpactTime)
                {
                    var info = body.runicLensDamageInfo;
                    var attacker = info != null ? info.attacker : null;
                    if (!attacker || !attacker.GetComponent<CharacterBody>())
                    {
                        body.runicLensMeteorReady = false;
                        _meteors++;
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        // Todo frame, por corpo com o item: só leituras de campo, sem GetComponent nem alocação.
        private void OnTransmitterUpdate(On.RoR2.TeleportOnLowHealthBehavior.orig_Update orig, TeleportOnLowHealthBehavior self)
        {
            try
            {
                var body = self.body;
                if (NetworkServer.active && body && !body.inventory)
                {
                    _transmitterUpdates++;
                    return;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        // Roda em toda máquina (o trecho de autoridade também usa o dono).
        private void OnOrbTriggerEnter(On.TeleportOnLowHealthAuraController.orig_OnTriggerEnter orig, TeleportOnLowHealthAuraController self, Collider other)
        {
            try
            {
                if (!self.owner)
                {
                    _orbContacts++;
                    return;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self, other);
        }

        private bool OnTeleportAwayFrom(On.RoR2.CharacterBody.orig_TeleportAwayFrom orig, CharacterBody self, Vector3 origin,
            out Vector3 destination, float targetDistance, float shellSize, Vector3? favoredForward)
        {
            try
            {
                var locator = self.modelLocator;
                if (!locator || !locator.modelTransform)
                {
                    _warps++;
                    destination = self.footPosition;
                    return false;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            return orig(self, origin, out destination, targetDistance, shellSize, favoredForward);
        }

        private void OnWarBondsDisable(On.RoR2.BarrageOnBossBehaviour.orig_OnDisable orig, BarrageOnBossBehaviour self)
        {
            try
            {
                orig(self);
            }
            catch (NullReferenceException) when (NoMaster(self))
            {
                _warBonds++; // a desinscrição que faltou era no master, que já não existe
            }
        }

        private static bool NoMaster(BarrageOnBossBehaviour self)
        {
            var body = self.body;
            return !body || !body.master;
        }

        // Todo tick, em todo corpo com EquipmentSlot: o campo bool vem primeiro para o caso comum não pagar nada.
        private void OnEquipmentFixedUpdate(On.RoR2.EquipmentSlot.orig_FixedUpdate orig, EquipmentSlot self)
        {
            try
            {
                var body = self.characterBody;
                if (body && body.requestEquipmentFire && !body.inventory)
                {
                    body.requestEquipmentFire = false;
                    _equipment++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        // Por spawn do diretor (não por tick): só leitura de dois floats no caso comum.
        private bool OnDirectorSpawn(On.RoR2.CombatDirector.orig_Spawn_SpawnArgs orig, CombatDirector self, CombatDirector.SpawnArgs args)
        {
            try
            {
                var rule = args.placementRule; // classe: a mudança chega ao spawn e às cópias do Enxame
                if (rule != null && rule.minDistanceSwarms == 0f && rule.maxDistanceSwarms == 0f && rule.maxDistance > 0f)
                {
                    rule.minDistanceSwarms = rule.minDistance;
                    rule.maxDistanceSwarms = rule.maxDistance;
                    _swarms++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            return orig(self, args);
        }
    }
}
