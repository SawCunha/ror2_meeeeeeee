using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

#pragma warning disable CS0618 // smallIcon*Sprite: campos obsoletos no 1.5.0, mas ainda lidos pelo jogo

namespace Ror2Meeeee
{
    // Artefato do Vazio: com ele ligado no lobby, cada estágio ganha ExtraChests baús do Void (Void Cradle) a mais, e todo
    // baú do Void passa a sortear entre TODOS os itens do Void (branco, verde, vermelho e boss do Void). O preço: todo
    // inimigo nasce com +50% de vida (5 stacks de BoostHp, +10% cada, somados a outros bônus de vida).
    // Baús: depois de o jogo popular o estágio (onPostPopulateSceneServer) e antes do Start do baú, que é onde ele sorteia.
    // Os baús do Void são reconhecidos pela tabela original do jogo (dtVoidChest). Só no host.
    // Precisa do DLC Survivors of the Void ativo na run: sem ele não há itens do Void para sortear.
    public sealed class VoidBountyFeature : Feature
    {
        public const string Name = "Ror2MeeeeeVoidBounty";
        public const string Token = "ROR2MEEEEE_ARTIFACT_VOIDBOUNTY";
        private const int HealthBoostStacks = 5; // BoostHp: +10% por stack

        private static ArtifactDef _def;
        private static BasicPickupDropTable _allVoid;   // criada no Awake: o jogo a regenera a cada início de run
        private static PickupDropTable _vanillaVoid;
        private static InteractableSpawnCard _voidChestCard;

        private readonly ConfigEntry<int> _extra;
        private Xoroshiro128Plus _rng; // do mod: não consome RNG da run

        public static void Register(ManualLogSource log)
        {
            try
            {
                _def = ScriptableObject.CreateInstance<ArtifactDef>();
                _def.cachedName = Name;
                _def.nameToken = Token + "_NAME";
                _def.descriptionToken = Token + "_DESC";
                _def.smallIconSelectedSprite = TitheArtifact.LoadIcon("vazio_selecionado.png", new Color32(170, 70, 200, 255));
                _def.smallIconDeselectedSprite = TitheArtifact.LoadIcon("vazio_desmarcado.png", new Color32(90, 90, 90, 255));
                LanguageAPI.Add(Token + "_NAME", "Artefato do Vazio");
                LanguageAPI.Add(Token + "_DESC",
                    "Mais baús do Vazio aparecem em cada estágio, e eles podem conter qualquer item do Vazio. " +
                    "Todos os inimigos têm 50% a mais de vida.");

                _allVoid = ScriptableObject.CreateInstance<BasicPickupDropTable>();
                _allVoid.name = "dtRor2MeeeeeAllVoid";
                _allVoid.hideFlags = HideFlags.DontUnloadUnusedAsset; // só o mod a referencia: a troca de cena a descarregaria
                _allVoid.tier1Weight = 0f;
                _allVoid.tier2Weight = 0f;
                _allVoid.tier3Weight = 0f;
                _allVoid.voidTier1Weight = 0.6f;  // pesos por raridade, como os baús do jogo
                _allVoid.voidTier2Weight = 0.3f;
                _allVoid.voidTier3Weight = 0.08f;
                _allVoid.voidBossWeight = 0.02f;
                _vanillaVoid = Addressables.LoadAssetAsync<PickupDropTable>("RoR2/Base/Common/dtVoidChest.asset").WaitForCompletion();
                _voidChestCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/VoidChest/iscVoidChest.asset").WaitForCompletion();
                if (!_vanillaVoid || !_voidChestCard) log.LogWarning("VoidBounty: dtVoidChest/iscVoidChest indisponíveis; o artefato fica só com a vida extra.");

                if (!ContentAddition.AddArtifactDef(_def)) { log.LogError("VoidBounty: ContentAddition recusou o artefato."); _def = null; }
                else log.LogInfo($"VoidBounty: artefato '{Name}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"VoidBounty: falha ao registrar o artefato (o plugin segue sem ele): {e}");
                _def = null;
            }
        }

        public static bool IsEnabled()
        {
            var mgr = RunArtifactManager.instance;
            return _def && mgr && mgr.IsArtifactEnabled(_def);
        }

        public VoidBountyFeature(Cfg cfg, ManualLogSource log) : base("VoidBounty", cfg, log,
            "Artefato do Vazio: com ele ativado no lobby, +ExtraChests baús do Vazio por estágio, todos sorteando entre todos os itens do Vazio; inimigos com +50% de vida.",
            enabledByDefault: true)
        {
            _extra = cfg.Bind("VoidBounty", "ExtraChests", 5, "Baús do Vazio a mais em cada estágio com o artefato ativo.");
        }

        public override void ResetRunState() { _rng = null; }

        protected override void OnEnable()
        {
            Track("SceneDirector.onPostPopulateSceneServer", () => SceneDirector.onPostPopulateSceneServer += OnPostPopulate);
            Track("CharacterMaster.onStartGlobal", () => CharacterMaster.onStartGlobal += OnMasterStart);
        }

        protected override void OnDisable()
        {
            SceneDirector.onPostPopulateSceneServer -= OnPostPopulate;
            CharacterMaster.onStartGlobal -= OnMasterStart;
        }

        private void OnPostPopulate(SceneDirector director)
        {
            try
            {
                var run = Run.instance;
                if (!NetworkServer.active || !run || !IsEnabled() || !_voidChestCard || !DirectorCore.instance) return;
                if (_rng == null) _rng = new Xoroshiro128Plus(run.seed ^ 0x564F494442545953UL); // "VOIDBTYS"

                var spawned = 0;
                for (var i = 0; i < Math.Max(0, _extra.Value); i++)
                {
                    var request = new DirectorSpawnRequest(_voidChestCard,
                        new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Random }, _rng);
                    if (DirectorCore.instance.TrySpawnObject(request)) spawned++;
                }

                // Todos os baús do Void do estágio (do jogo e os extras): o sorteio acontece no Start, ainda não rodou.
                var replaced = 0;
                var chests = InstanceTracker.GetInstancesList<ChestBehavior>();
                for (var i = 0; i < chests.Count; i++)
                {
                    var c = chests[i];
                    if (c && c.dropTable == _vanillaVoid) { c.dropTable = _allVoid; replaced++; }
                }
                Log.LogInfo($"[VoidBounty] estágio: +{spawned} baús do Vazio; {replaced} baú(s) sorteando entre todos os itens do Vazio.");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnMasterStart(CharacterMaster master)
        {
            try
            {
                if (!NetworkServer.active || !master || master.playerCharacterMasterController || !IsEnabled()) return;
                var team = master.teamIndex;
                if (team != TeamIndex.Monster && team != TeamIndex.Lunar && team != TeamIndex.Void) return;
                var inv = master.inventory;
                if (inv) inv.GiveItemPermanent(RoR2Content.Items.BoostHp, HealthBoostStacks);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
