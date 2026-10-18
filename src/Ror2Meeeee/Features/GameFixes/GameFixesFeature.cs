using System;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
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
    //  - Craca do Vazio (VoidBarnacle), NoCastSpawn: o efeito de spawn (EntityStateGameObject do 1.5.0) vem sem endereço
    //    nem referência; o teste `!= null` olha o invólucro e o jogo chama SpawnEffect com prefab nulo, logando erro com
    //    pilha a cada craca. Sem efeito de verdade, o campo é zerado antes de orig (uma vez por spawn; sem carga de asset).
    //  - Defective Unit, DeathState.AttemptDeathBehavior: lê cachedModelTransform.transform sem checar; morrer já sem
    //    modelo estoura. Sem modelo, o método só faria o efeito (sem posição) e DestroyModel (que não faz nada): pulado.
    //  - Dunestrider (ClayBoss), DeathState.AttemptDeathBehavior: centerTransform nulo (modelo sem "Center") estoura no
    //    efeito ANTES de destruir modelo e corpo, e o método só roda uma vez: o corpo morto ficava no servidor para
    //    sempre. Sem centro, ele passa a ser o transform do próprio corpo e o método do jogo segue inteiro.
    //  - Baú Mímico, Chest.BreakOpen/Burrow.OnEnter: lê a posição do "Muzzle" antes do `if (transform)` que o protege; sem
    //    o ponto, estoura. No BreakOpen isso pula o fakeActorCounter++ do fim, mas o OnExit subtrai: o contador fica em -1
    //    e a camada de colisão do mímico (ator falso) fica errada. Só sem Muzzle: a exceção esperada é tratada e o
    //    incremento pulado é feito (no Burrow, depois do efeito, não falta nada).
    // Exceção à regra de sempre chamar orig: Update do Transmitter, OnTriggerEnter e TeleportAwayFrom só pulam orig
    // quando ele lançaria a exceção com certeza (mesmo padrão do GhostBladeFeature).
    public sealed class GameFixesFeature : Feature
    {
        private long _meteors, _transmitterUpdates, _orbContacts, _warps, _warBonds, _equipment, _swarms, _barnacles, _defective, _clayBoss, _mimic, _homing, _parry;

        public GameFixesFeature(Cfg cfg, ManualLogSource log) : base("GameFixes", cfg, log,
            "Fixes game errors that repeated hundreds of times per match and cost FPS: Runic Lens, Unstable Transmitter, War Bonds and equipment on bodies without an inventory. Also fixes the Swarms artifact (copies spawned on top of the player or didn't spawn) and Golden drones/monsters that froze the match with so many gold nuggets. Does not change the game otherwise.", "Corrige erros do jogo que se repetiam centenas de vezes por partida e custavam FPS: Runic Lens, Unstable Transmitter, War Bonds e equipamento de corpos sem inventário. Também conserta o artefato do Enxame (cópias nasciam em cima do jogador ou não nasciam) e drones/monstros Dourados que travavam a partida de tanta pepita de ouro. Não muda o jogo além disso.",
            enabledByDefault: true)
        {
        }

        public override void ResetRunState()
        {
            var total = _meteors + _transmitterUpdates + _orbContacts + _warps + _warBonds + _equipment + _swarms + _barnacles + _defective + _clayBoss + _mimic + _homing + _parry;
            if (total > 0)
                Log.LogInfo($"[GameFixes] run: Runic Lens {_meteors}, Transmitter Update {_transmitterUpdates}, orbe sem dono {_orbContacts}, "
                    + $"teleporte sem modelo {_warps}, War Bonds {_warBonds}, equipamento {_equipment}, craca do Vazio {_barnacles}, Defective Unit {_defective}, Dunestrider {_clayBoss}, Baú Mímico {_mimic}, mira de projétil/caldeirão no cliente {_homing}, aparo do Enforcer {_parry} erro(s) evitados; "
                    + $"Enxame: {_swarms} spawn(s) corrigidos.");
            _meteors = _transmitterUpdates = _orbContacts = _warps = _warBonds = _equipment = _swarms = _barnacles = _defective = _clayBoss = _mimic = _homing = _parry = 0;
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
            Track("VoidBarnacle.NoCastSpawn.OnEnter", () => On.EntityStates.VoidBarnacle.NoCastSpawn.OnEnter += OnBarnacleSpawn);
            if (_cachedModel == null) _cachedModel = MakeCachedModelGetter();
            if (_cachedModel != null)
                Track("DefectiveUnit.DeathState.AttemptDeathBehavior", () => On.EntityStates.DefectiveUnit.DeathState.AttemptDeathBehavior += OnDefectiveDeath);
            else
                Log.LogWarning("[GameFixes] Defective Unit: cachedModelTransform não encontrado; correção não aplicada.");
            if (ClayBossCenter == null)
                Log.LogWarning("[GameFixes] Dunestrider: centerTransform não encontrado; correção não aplicada.");
            else
                Track("ClayBoss.DeathState.AttemptDeathBehavior", () => On.EntityStates.ClayBoss.DeathState.AttemptDeathBehavior += OnClayBossDeath);
            Track("Mimic.Chest.BreakOpen.OnEnter", () => On.EntityStates.Mimic.Chest.BreakOpen.OnEnter += OnMimicBreakOpen);
            Track("Mimic.Chest.Burrow.OnEnter", () => On.EntityStates.Mimic.Chest.Burrow.OnEnter += OnMimicBurrow);
            Track("ProjectileSteerTowardTarget.FixedUpdate", () => On.RoR2.Projectile.ProjectileSteerTowardTarget.FixedUpdate += OnSteerFixedUpdate);
            Track("ProjectileSphereTargetFinder.FixedUpdate", () => On.RoR2.Projectile.ProjectileSphereTargetFinder.FixedUpdate += OnSphereFinderFixedUpdate);
            Track("HallowedCauldron.DrainingItems.FixedUpdate", () => On.EntityStates.HallowedCauldron.DrainingItems.FixedUpdate += OnCauldronDrainFixedUpdate);
            Track("Enforcer.ProtectAndServeDeactivate.FireDamageOrbServer", () => On.EntityStates.Enforcer.ProtectAndServeDeactivate.FireDamageOrbServer += OnParryOrb);
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
            On.EntityStates.VoidBarnacle.NoCastSpawn.OnEnter -= OnBarnacleSpawn;
            On.EntityStates.DefectiveUnit.DeathState.AttemptDeathBehavior -= OnDefectiveDeath;
            On.EntityStates.ClayBoss.DeathState.AttemptDeathBehavior -= OnClayBossDeath;
            On.EntityStates.Mimic.Chest.BreakOpen.OnEnter -= OnMimicBreakOpen;
            On.EntityStates.Mimic.Chest.Burrow.OnEnter -= OnMimicBurrow;
            On.RoR2.Projectile.ProjectileSteerTowardTarget.FixedUpdate -= OnSteerFixedUpdate;
            On.RoR2.Projectile.ProjectileSphereTargetFinder.FixedUpdate -= OnSphereFinderFixedUpdate;
            On.EntityStates.HallowedCauldron.DrainingItems.FixedUpdate -= OnCauldronDrainFixedUpdate;
            On.EntityStates.Enforcer.ProtectAndServeDeactivate.FireDamageOrbServer -= OnParryOrb;
        }

        // Mesmo caminho do GetModelChildLocator (protegido) do jogo.
        private static bool MissingMuzzle(EntityStates.EntityState self)
        {
            var locator = self.outer ? self.outer.GetComponent<ModelLocator>() : null;
            var model = locator ? locator.modelTransform : null;
            var children = model ? model.GetComponent<ChildLocator>() : null;
            return !children || !children.FindChild("Muzzle");
        }

        // Uma vez por mudança de forma do mímico. Com Muzzle, orig roda sem nada em volta de diferente.
        private void OnMimicBreakOpen(On.EntityStates.Mimic.Chest.BreakOpen.orig_OnEnter orig, EntityStates.Mimic.Chest.BreakOpen self)
        {
            var missing = MissingMuzzle(self);
            try
            {
                orig(self);
            }
            catch (NullReferenceException) when (missing)
            {
                var body = self.outer ? self.outer.GetComponent<CharacterBody>() : null; // characterBody do estado é protegido
                if (body) body.fakeActorCounter++; // a linha que o estouro pulou
                _mimic++;
            }
        }

        private void OnMimicBurrow(On.EntityStates.Mimic.Chest.Burrow.orig_OnEnter orig, EntityStates.Mimic.Chest.Burrow self)
        {
            var missing = MissingMuzzle(self);
            try
            {
                orig(self);
            }
            catch (NullReferenceException) when (missing)
            {
                _mimic++; // o estouro é no efeito, o último passo: a lógica do servidor já rodou
            }
        }

        // Campo privado; null se o jogo mudar (a correção fica de fora, sem derrubar a feature).
        private static readonly System.Reflection.FieldInfo ClayBossCenter = typeof(EntityStates.ClayBoss.DeathState)
            .GetField("centerTransform", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Uma vez por morte de Dunestrider; orig sempre roda (é ele que destrói o corpo).
        private void OnClayBossDeath(On.EntityStates.ClayBoss.DeathState.orig_AttemptDeathBehavior orig, EntityStates.ClayBoss.DeathState self)
        {
            try
            {
                if (self.outer && !(Transform)ClayBossCenter.GetValue(self))
                {
                    ClayBossCenter.SetValue(self, self.outer.transform);
                    _clayBoss++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        // cachedModelTransform é protegido; delegate do getter criado uma vez (a leitura pública via modelLocator não
        // serve: o OnEnter da base pode zerar o cache no mesmo frame em que o Destroy do modelo ainda não aconteceu).
        // Criado no OnEnable: se o jogo mudar a propriedade, só esta correção fica de fora (sem derrubar a feature).
        private static Func<EntityStates.GenericCharacterDeath, Transform> _cachedModel;

        private static Func<EntityStates.GenericCharacterDeath, Transform> MakeCachedModelGetter()
        {
            var getter = typeof(EntityStates.GenericCharacterDeath).GetProperty("cachedModelTransform",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetGetMethod(true);
            return getter == null ? null : (Func<EntityStates.GenericCharacterDeath, Transform>)Delegate.CreateDelegate(
                typeof(Func<EntityStates.GenericCharacterDeath, Transform>), getter);
        }

        // Uma vez por morte. Só pula orig quando ele lançaria com certeza (sem modelo); o resto segue o jogo.
        private void OnDefectiveDeath(On.EntityStates.DefectiveUnit.DeathState.orig_AttemptDeathBehavior orig, EntityStates.DefectiveUnit.DeathState self)
        {
            try
            {
                if (!_cachedModel(self))
                {
                    _defective++;
                    return;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        // Roda em toda máquina (o efeito é local, transmit: false). Lê só os campos do invólucro: a conversão implícita
        // para GameObject dispararia uma carga do Addressables.
        private void OnBarnacleSpawn(On.EntityStates.VoidBarnacle.NoCastSpawn.orig_OnEnter orig, EntityStates.VoidBarnacle.NoCastSpawn self)
        {
            try
            {
                var fx = self.spawnFXPrefab;
                if (fx != null && !fx.directReference && (fx.addressableReference == null || !fx.addressableReference.RuntimeKeyIsValid()))
                {
                    self.spawnFXPrefab = null;
                    _barnacles++;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
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

        // Mira de projétil (teleguiado/minas) no cliente: o Start desliga o componente fora do host (sem preencher os campos),
        // mas o jogo o religa em alguns caminhos (Retorno do Cyclone da Huntress, objetos soltos do Drifter) e cada tick
        // estoura NullReferenceException (log de um cliente: ~10.700 em uma run). Fora do host, desliga de novo, como o
        // próprio Start faria. No host é só um bool estático por tick.
        private void OnSteerFixedUpdate(On.RoR2.Projectile.ProjectileSteerTowardTarget.orig_FixedUpdate orig, ProjectileSteerTowardTarget self)
        {
            if (!NetworkServer.active && (!self.useAuthorityNotServer || !Util.HasEffectiveAuthority(self.gameObject)))
            {
                self.enabled = false;
                _homing++;
                return;
            }
            orig(self);
        }

        private void OnSphereFinderFixedUpdate(On.RoR2.Projectile.ProjectileSphereTargetFinder.orig_FixedUpdate orig, ProjectileSphereTargetFinder self)
        {
            if (!NetworkServer.active)
            {
                self.enabled = false;
                _homing++;
                return;
            }
            orig(self);
        }

        // Caldeirão/santuário que drena itens: no cliente o estado não tem o "activator" (só o host recebe) e o
        // CheckIfDoneDraining chama IsAffordable com ele nulo a cada tick (NRE; log de um cliente: 251). Sem activator no
        // cliente, o tick não faz nada útil (a barra vem do host e quem troca o estado é o host): pula.
        private void OnCauldronDrainFixedUpdate(On.EntityStates.HallowedCauldron.DrainingItems.orig_FixedUpdate orig, EntityStates.HallowedCauldron.DrainingItems self)
        {
            if (!NetworkServer.active && !self.activator)
            {
                _homing++;
                return;
            }
            orig(self);
        }

        // Aparo do Enforcer: o contra-ataque mira o atacante do golpe aparado, mas o golpe pode vir sem atacante ou de um
        // corpo já destruído (morreu; com o Dio's da Tormenta, renasce em outro corpo). O jogo faz GetComponent no nulo e a
        // exceção pula a linha que invalida o golpe: o mesmo erro repete a cada tick da janela e o aparo não dispara.
        // Sem atacante vivo não há em quem revidar: pula só o revide (o resto do aparo segue).
        private void OnParryOrb(On.EntityStates.Enforcer.ProtectAndServeDeactivate.orig_FireDamageOrbServer orig, EntityStates.Enforcer.ProtectAndServeDeactivate self, DamageInfo damageInfo)
        {
            if (damageInfo == null || !damageInfo.attacker)
            {
                _parry++;
                return;
            }
            orig(self, damageInfo);
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
