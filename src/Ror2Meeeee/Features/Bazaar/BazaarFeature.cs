using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Bazaar;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Bazar Maior (reescrita do BiggerBazaar 1.14.5, de MagnusMagnuson): o Bazar entre Tempos ganha até 6 baús com
    // itens à mostra, comprados com o ouro que sobrou do estágio (ou com moedas lunares), e um casulo lunar que troca
    // moeda lunar por ouro. Só no host.
    //  - Ouro: ao entrar no portal do bazar (SceneExitController.onBeginExit, antes da conversão em XP), o ouro de cada
    //    jogador é guardado e zerado; volta ao chegar no bazar. O que sobrar vira XP ao sair, como no jogo.
    //  - Baús: SpawnCard direto nas posições fixas do mapa, com onPurchase vazio (não abrem nem ficam indisponíveis);
    //    a entrega é no PurchaseHub (passo 0). O item à mostra é um GenericPickup com waitDuration infinito (o trigger e
    //    a interação do servidor só concedem depois dele; `consumed` não serve, o Start() do jogo o zera) e recycled (o
    //    Reciclador não o troca); sem hook em AttemptGrant.
    //  - Limite de trocas no casulo por jogador: hook em PurchaseInteraction.GetInteractability (chama orig; o servidor
    //    confere antes de comprar). Compras nos baús só têm o estoque de cada baú.
    //  - Captain: o hack do sinalizador não abre os baús do bazar (compra grátis furaria o estoque).
    //  - Os dois hooks só existem no host durante a visita ao bazar (SetHooks): fora dele, custo zero por frame.
    // Diferenças do original: sorteio próprio (Xoroshiro, não consome RNG da run), preço = baú pequeno do estágio
    // anterior x multiplicador (o original escalava duas vezes), sem Sale Star consumida à toa, sem fogos no casulo,
    // itens únicos do mod fora da loja. Com o Comando, o item comprado vira o cubo de escolha do mesmo tier (só a vitrine
    // ignora o artefato). Sem ShareSuite: o item cai no chão e passa pelo Multiplicador/Comunismo como qualquer pickup.
    public sealed class BazaarFeature : Feature
    {
        private const string Section = "Bazaar";

        // Coordenadas do mapa do bazar (do original, conferidas em jogo por ele): balcão à direita do Newt.
        private static readonly Vector3[] ChestPositions =
        {
            new Vector3(-99.1f, -26f, -54.6f), new Vector3(-95f, -26.2f, -57.7f), new Vector3(-90.9f, -26f, -60.7f),
            new Vector3(-86.5f, -26.2f, -63.8f), new Vector3(-82.3f, -25f, -67f), new Vector3(-78f, -24.7f, -70.3f),
        };
        private static readonly float[] ChestYaw = { 34.4f, 35.2f, 36f, 37.4f, 38.2f, 37.2f };
        private static readonly Vector3 PodPosition = new Vector3(-118.9f, -23.4f, -45.4f);
        private static readonly Vector3 DisplayOffset = new Vector3(-2f, 4f, -3f);

        private static readonly string[] TierKeys = { "White", "Green", "Red", "Boss", "Lunar", "Equipment", "LunarEquipment" };
        private static readonly string[] TierNames = { "brancos", "verdes", "vermelhos", "de chefe (amarelos)", "lunares", "equipamentos", "equipamentos lunares" };
        private static readonly float[] DefaultWeight = { 0.55f, 0.3f, 0.15f, 0f, 0f, 0f, 0f };
        private static readonly float[] DefaultCost = { 1f, 2f, 16f, 24f, 16f, 8f, 16f };
        private static readonly int[] DefaultLunarCost = { 1, 2, 3, 5, 3, 3, 3 };
        private static readonly int[] DefaultStock = { 10, 10, 10, 10, 10, 10, 10 };

        private sealed class Offer
        {
            public UniquePickup Pickup;
            public int Stock;
            public int Sold;
            public int Drops; // 1, ou N itens temporários
            public GameObject Display;
        }

        private readonly ConfigEntry<bool> _lunarMode;
        private readonly ConfigEntry<bool> _keepMoney;
        private readonly ConfigEntry<int> _chests;
        private readonly ConfigEntry<int> _coinWorth;
        private readonly ConfigEntry<int> _maxExchanges;
        private readonly ConfigEntry<int> _tempChance;
        private readonly ConfigEntry<int> _tempAmount;
        private readonly ConfigEntry<bool> _announce;
        private readonly ConfigEntry<float>[] _weight = new ConfigEntry<float>[BazaarRules.TierCount];
        private readonly ConfigEntry<float>[] _cost = new ConfigEntry<float>[BazaarRules.TierCount];
        private readonly ConfigEntry<int>[] _lunarCost = new ConfigEntry<int>[BazaarRules.TierCount];
        private readonly ConfigEntry<int>[] _stock = new ConfigEntry<int>[BazaarRules.TierCount];

        private readonly Dictionary<GameObject, Offer> _offers = new Dictionary<GameObject, Offer>();
        private readonly Dictionary<CharacterMaster, uint> _stash = new Dictionary<CharacterMaster, uint>();
        private readonly Dictionary<CharacterMaster, int> _exchanges = new Dictionary<CharacterMaster, int>();
        private readonly float[] _weights = new float[BazaarRules.TierCount];
        private readonly List<PickupIndex> _candidates = new List<PickupIndex>();
        private GameObject _pod;
        private PurchaseInteraction _podPi; // comparado direto no hook por frame (sem .gameObject)
        private bool _hooked;
        private static GameObject _coinEffect;
        private float _coef; // coeficiente de dificuldade do último estágio normal (preço dos baús dele)
        private float _bazaarCoef;
        private Xoroshiro128Plus _rng;

        public BazaarFeature(Cfg cfg, ManualLogSource log) : base(Section, cfg, log,
            "Bazar Maior: o Bazar entre Tempos ganha baús com itens à mostra, comprados com o ouro que sobrou do estágio (ou moedas lunares), e um casulo que troca moeda lunar por ouro.",
            enabledByDefault: true)
        {
            _lunarMode = cfg.Bind(Section, "PayWithLunarCoins", false, "Os baús do bazar custam moedas lunares (preços LunarCost de cada tier) em vez de ouro.");
            _keepMoney = cfg.Bind(Section, "KeepMoney", true, "Ao entrar no portal do bazar, o ouro NÃO vira XP: fica para gastar no bazar. O que sobrar vira XP ao sair. Desligado: o ouro vira XP como no jogo.");
            _chests = cfg.Bind(Section, "Chests", BazaarRules.MaxChests, $"Quantos baús aparecem (1 a {BazaarRules.MaxChests}).");
            _coinWorth = cfg.Bind(Section, "LunarCoinGold", 50, "Ouro por moeda lunar no casulo ao lado do Newt (escala com a dificuldade como os baús; 25 = um baú pequeno, 50 = um médio).");
            _maxExchanges = cfg.Bind(Section, "MaxExchangesPerPlayer", 3, "Quantas trocas de moeda lunar por ouro cada jogador pode fazer por visita. 0 = sem casulo; -1 = sem limite.");
            _tempChance = cfg.Bind(Section, "TemporaryChance", 10, "Chance (%) de um baú vender itens temporários: entrega TemporaryAmount cópias que somem com o tempo. Equipamentos nunca.");
            _tempAmount = cfg.Bind(Section, "TemporaryAmount", 5, "Cópias entregues por compra num baú de itens temporários.");
            _announce = cfg.Bind(Section, "Announce", true, "Avisa no chat, ao chegar no bazar, quantos baús estão à venda e quanto ouro o casulo dá por moeda lunar.");
            for (var i = 0; i < BazaarRules.TierCount; i++)
            {
                var k = TierKeys[i];
                var n = TierNames[i];
                _weight[i] = cfg.Bind(Section, k + "Weight", DefaultWeight[i], $"Peso relativo de um baú vender itens {n} (0 = nunca).");
                _cost[i] = cfg.Bind(Section, k + "Cost", DefaultCost[i], $"Preço dos itens {n} em baús pequenos do estágio anterior (1 = um baú pequeno, 2 = um médio, 16 = um lendário).");
                _lunarCost[i] = cfg.Bind(Section, k + "LunarCost", DefaultLunarCost[i], $"Preço dos itens {n} em moedas lunares (com PayWithLunarCoins).");
                _stock[i] = cfg.Bind(Section, k + "Stock", DefaultStock[i], $"Quantas vezes um baú de itens {n} pode ser comprado. -1 = sem limite.");
            }
        }

        protected override void OnEnable()
        {
            Track("SceneExitController.onBeginExit", () => SceneExitController.onBeginExit += OnBeginExit);
            Track("SceneDirector.onPostPopulateSceneServer", () => SceneDirector.onPostPopulateSceneServer += OnPostPopulate);
            Track("PurchaseInteraction.onPurchaseGlobalServer", () => PurchaseHub.SetBazaar(OnPurchase)); // passo 0
            // GetInteractability e o hack do Captain: ligados por SetHooks só durante a visita ao bazar.
        }

        protected override void OnDisable()
        {
            SceneExitController.onBeginExit -= OnBeginExit;
            SceneDirector.onPostPopulateSceneServer -= OnPostPopulate;
            PurchaseHub.SetBazaar(null);
            RestoreMoney(); // desligar entre o portal e o bazar não pode sumir com o ouro
            // Desligar dentro do bazar: os baús não abrem sozinhos (onPurchase vazio); sem a feature, cobrariam sem entregar.
            if (NetworkServer.active)
            {
                foreach (var chest in _offers.Keys) CloseShop(chest);
                CloseShop(_pod);
            }
            ClearVisit();
        }

        public override void ResetRunState()
        {
            _stash.Clear();
            ClearVisit();
            _rng = null;
            _coef = 0f;
        }

        private static void CloseShop(GameObject go)
        {
            var pi = go ? go.GetComponent<PurchaseInteraction>() : null;
            if (pi) pi.SetAvailable(false);
        }

        private static bool IsBazaar(SceneDef def) => def && def.baseSceneName == "bazaar";

        private bool InVisit => _offers.Count > 0 || _pod;

        private void ClearVisit()
        {
            _offers.Clear();
            _exchanges.Clear();
            _pod = null;
            _podPi = null;
            SetHooks(false);
        }

        private void SetHooks(bool on)
        {
            if (on == _hooked) return;
            if (on)
            {
                On.RoR2.PurchaseInteraction.GetInteractability += OnGetInteractability;
                On.EntityStates.CaptainSupplyDrop.HackingMainState.PurchaseInteractionIsValidTarget += OnHackTarget;
            }
            else
            {
                On.RoR2.PurchaseInteraction.GetInteractability -= OnGetInteractability;
                On.EntityStates.CaptainSupplyDrop.HackingMainState.PurchaseInteractionIsValidTarget -= OnHackTarget;
            }
            _hooked = on;
            if (Cfg.VerboseLogging) Log.LogDebug($"[Bazaar] hooks da loja {(on ? "ligados" : "desligados")}");
        }

        // ---- ouro: guardado no portal, devolvido no bazar ----

        private void OnBeginExit(SceneExitController exit)
        {
            try
            {
                if (!NetworkServer.active || !_keepMoney.Value || !exit) return;
                if (!IsBazaar(exit.destinationScene) || IsBazaar(SceneCatalog.mostRecentSceneDef)) return;
                _stash.Clear();
                var players = PlayerCharacterMasterController.instances;
                for (var i = 0; i < players.Count; i++)
                {
                    var m = players[i] ? players[i].master : null;
                    if (!m) continue;
                    _stash[m] = m.money;
                    m.money = 0; // direto, sem GiveMoney: Taxade/Dízimo não contam isso como ganho
                }
                Log.LogInfo($"[Bazaar] portal do bazar: ouro de {_stash.Count} jogador(es) guardado (não vira XP).");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void RestoreMoney()
        {
            foreach (var kv in _stash)
                if (kv.Key) kv.Key.money = BazaarRules.AddMoney(kv.Key.money, kv.Value);
            _stash.Clear();
        }

        // ---- montagem da loja ----

        private void OnPostPopulate(SceneDirector director)
        {
            try
            {
                var run = Run.instance;
                if (!NetworkServer.active || !run) return;
                ClearVisit(); // objetos da visita anterior morreram com a cena
                if (!IsBazaar(SceneCatalog.mostRecentSceneDef))
                {
                    _coef = run.difficultyCoefficient;
                    RestoreMoney(); // portal que não chegou no bazar: o ouro volta
                    return;
                }
                RestoreMoney();
                SetUpShop(run);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void SetUpShop(Run run)
        {
            if (_rng == null) _rng = new Xoroshiro128Plus(run.seed ^ 0x42415A4141524D45UL); // "BAZAARME"
            _bazaarCoef = _coef > 0f ? _coef : run.difficultyCoefficient; // começar a run no bazar: sem estágio anterior
            var lunar = _lunarMode.Value;

            for (var i = 0; i < BazaarRules.TierCount; i++)
                _weights[i] = Pool(run, (BazaarTier)i).Count > 0 ? _weight[i].Value : 0f;

            var card = LegacyResourcesAPI.Load<SpawnCard>(lunar
                ? "SpawnCards/InteractableSpawnCard/iscLunarChest"
                : "SpawnCards/InteractableSpawnCard/iscChest1");
            if (!card) { Log.LogWarning("[Bazaar] spawn card do baú indisponível; loja não montada."); return; }

            var n = BazaarRules.ClampChests(_chests.Value);
            for (var i = 0; i < n; i++)
            {
                var tier = BazaarRules.PickWeighted(_weights, _rng.nextNormalizedFloat);
                if (tier < 0) break; // nenhum tier com peso e itens
                FillCandidates(Pool(run, (BazaarTier)tier));
                if (_candidates.Count == 0) continue;
                SpawnOffer(card, i, tier, _rng.NextElementUniform(_candidates), lunar);
            }
            _candidates.Clear();

            if (_maxExchanges.Value != 0) SpawnPod();
            if (InVisit) SetHooks(true);

            Log.LogInfo($"[Bazaar] loja montada: {_offers.Count} baú(s), {(lunar ? "moedas lunares" : $"ouro (coef {_bazaarCoef:F2})")}, casulo {(_pod ? "sim" : "não")}.");
            if (_announce.Value && Plugin.Instance) Plugin.Instance.StartCoroutine(Announce(BuildAnnouncement()));
        }

        private static List<PickupIndex> Pool(Run run, BazaarTier tier)
        {
            switch (tier)
            {
                case BazaarTier.White: return run.availableTier1DropList;
                case BazaarTier.Green: return run.availableTier2DropList;
                case BazaarTier.Red: return run.availableTier3DropList;
                case BazaarTier.Boss: return run.availableBossDropList;
                case BazaarTier.Lunar: return run.availableLunarItemDropList;
                case BazaarTier.Equipment: return run.availableEquipmentDropList;
                default: return run.availableLunarEquipmentDropList;
            }
        }

        // Itens únicos do mod (Capitalismo, Cano Extra, Chamado da Horda) ficam fora: o Taxade troca a cópia repetida
        // no baú, e aqui o item sai direto do bazar.
        private void FillCandidates(List<PickupIndex> pool)
        {
            _candidates.Clear();
            for (var i = 0; i < pool.Count; i++)
            {
                var def = PickupCatalog.GetPickupDef(pool[i]);
                if (def != null && !ItemClassifier.IsUnique(def.itemIndex)) _candidates.Add(pool[i]);
            }
        }

        private void SpawnOffer(SpawnCard card, int slot, int tier, PickupIndex index, bool lunar)
        {
            var chest = Spawn(card, ChestPositions[slot], ChestYaw[slot]);
            var pi = chest ? chest.GetComponent<PurchaseInteraction>() : null;
            if (!pi) { if (chest) UnityEngine.Object.Destroy(chest); return; }

            if (lunar)
            {
                pi.NetworkcostType = CostTypeIndex.LunarCoin;
                pi.Networkcost = Math.Max(0, _lunarCost[tier].Value);
            }
            else
            {
                pi.NetworkcostType = CostTypeIndex.Money;
                pi.Networkcost = BazaarRules.GoldCost(_bazaarCoef, _cost[tier].Value);
            }

            var def = PickupCatalog.GetPickupDef(index);
            var equipment = def != null && def.equipmentIndex != EquipmentIndex.None;
            var temporary = !equipment && _rng.nextNormalizedFloat * 100f < _tempChance.Value;
            // O comprado segue os artefatos: com o Comando, cai o cubo de escolha do mesmo tier, como num baú do jogo.
            var pickup = new UniquePickup(index) { decayValue = temporary ? 1f : 0f }; // 1 = valor dos baús temporários

            var shown = pickup;
            shown.ignoreArtifactBasedPickupReplacements = true; // a vitrine mostra o item, não um cubo do Comando
            var info = new GenericPickupController.CreatePickupInfo
            {
                position = ChestPositions[slot] + DisplayOffset,
                rotation = Quaternion.identity,
                pickup = shown,
                recycled = true, // o Reciclador ignora
            };
            var display = GenericPickupController.CreatePickup(in info);
            // Servidor nunca concede: trigger e interação esperam waitDuration, que o Start() não reinicia.
            if (display) display.waitDuration = float.PositiveInfinity;

            _offers[chest] = new Offer
            {
                Pickup = pickup,
                Stock = _stock[tier].Value,
                Drops = temporary ? Math.Max(1, _tempAmount.Value) : 1,
                Display = display ? display.gameObject : null,
            };
        }

        private void SpawnPod()
        {
            var card = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscLunarChest");
            var pod = card ? Spawn(card, PodPosition, 0f) : null;
            var pi = pod ? pod.GetComponent<PurchaseInteraction>() : null;
            if (!pi) { if (pod) UnityEngine.Object.Destroy(pod); return; }
            pi.disableSpawnOnInteraction = true; // troca não dispara Fogos de Artifício, Lula etc.
            _pod = pod;
            _podPi = pi;
        }

        // Baú que não abre: compra sem SetAvailable(false) nem Open (onPurchase vazio); a entrega é no OnPurchase.
        private GameObject Spawn(SpawnCard card, Vector3 position, float yaw)
        {
            var rule = new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Direct };
            var go = card.DoSpawn(position, Quaternion.identity, new DirectorSpawnRequest(card, rule, _rng)).spawnedInstance;
            if (!go) return null;
            go.transform.eulerAngles = new Vector3(0f, yaw, 0f); // o card pode girar/alinhar ao chão
            var pi = go.GetComponent<PurchaseInteraction>();
            if (pi)
            {
                // Os dois eventos: os prefabs podem ligar Open/SetAvailable em qualquer um deles.
#pragma warning disable CS0618 // onPurchase: obsoleto no 1.5.0, mas o jogo ainda o invoca
                pi.onPurchase = new PurchaseEvent();
#pragma warning restore CS0618
                pi.onDetailedPurchaseServer = new DetailedPurchaseEvent();
                pi.saleStarCompatible = false; // a Sale Star mudaria o dropCount do baú, que aqui não é usado
            }
            return go;
        }

        private string BuildAnnouncement()
        {
            var text = $"<color=#e5c962>Bazar Maior</color>: {_offers.Count} baú(s) à venda.";
            if (_pod)
            {
                var gold = BazaarRules.ScaledCost(_coinWorth.Value, _bazaarCoef, 1f);
                var ex = _maxExchanges.Value;
                text += $" Casulo lunar: 1 moeda = ${gold}" + (ex < 0 ? "." : $" (até {ex}x).");
            }
            return text;
        }

        private static IEnumerator Announce(string text)
        {
            yield return new WaitForSeconds(3f); // dá tempo de os clientes carregarem a cena
            if (IsBazaar(SceneCatalog.mostRecentSceneDef))
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = text });
        }

        // ---- compra ----

        // O contexto vem de um pool do jogo: nada é guardado depois do callback.
        private void OnPurchase(CostTypeDef.PayCostContext ctx, CostTypeDef.PayCostResults results)
        {
            try
            {
                if (!NetworkServer.active || !InVisit) return;
                var go = ctx.purchasedObject;
                var master = ctx.activatorMaster;
                if (!go || !master) return;

                if (go == _pod)
                {
                    var gold = BazaarRules.ScaledCost(_coinWorth.Value, _bazaarCoef, 1f);
                    master.money = BazaarRules.AddMoney(master.money, gold); // direto: troca não é ganho para Taxade/Dízimo
                    Bump(_exchanges, master);
                    if (!_coinEffect) _coinEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/CoinEmitter");
                    if (_coinEffect) EffectManager.SpawnEffect(_coinEffect, new EffectData { origin = go.transform.position, genericFloat = gold }, true);
                    Util.PlaySound("Play_UI_coin", go);
                    return;
                }

                if (!_offers.TryGetValue(go, out var offer)) return;
                offer.Sold++;

                var origin = go.transform.position + Vector3.up * 1.5f;
                var velocity = Vector3.up * 20f + go.transform.forward * 2f;
                var turn = Quaternion.AngleAxis(360f / offer.Drops, Vector3.up);
                for (var i = 0; i < offer.Drops; i++)
                {
                    PickupDropletController.CreatePickupDroplet(offer.Pickup, origin, velocity, isDuplicated: false);
                    velocity = turn * velocity;
                }

                if (!BazaarRules.UnderLimit(offer.Sold, offer.Stock))
                {
                    if (ctx.purchaseInteraction) ctx.purchaseInteraction.SetAvailable(false);
                    if (offer.Display) UnityEngine.Object.Destroy(offer.Display);
                }
                if (Cfg.VerboseLogging) Log.LogDebug($"[Bazaar] {Util.GetBestMasterName(master)} comprou {go.name} ({offer.Sold}/{offer.Stock})");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private static void Bump(Dictionary<CharacterMaster, int> counts, CharacterMaster master)
        {
            counts.TryGetValue(master, out var n);
            counts[master] = n + 1;
        }

        private static int Count(Dictionary<CharacterMaster, int> counts, CharacterMaster master) =>
            counts.TryGetValue(master, out var n) ? n : 0;

        // Só no host durante a visita, mas roda a cada frame para a interação olhada: sai cedo por referência.
        private Interactability OnGetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
        {
            var result = orig(self, activator);
            if (result != Interactability.Available || (object)self != _podPi || _maxExchanges.Value < 0) return result;
            try
            {
                var body = activator ? activator.GetComponent<CharacterBody>() : null;
                var master = body ? body.master : null;
                if (!master) return result;
                return BazaarRules.UnderLimit(Count(_exchanges, master), _maxExchanges.Value) ? result : Interactability.ConditionsNotMet;
            }
            catch (Exception e)
            {
                Fail(e);
                return result;
            }
        }

        private bool OnHackTarget(On.EntityStates.CaptainSupplyDrop.HackingMainState.orig_PurchaseInteractionIsValidTarget orig, PurchaseInteraction pi)
        {
            var valid = orig(pi);
            return valid && !(pi && (pi == _podPi || _offers.ContainsKey(pi.gameObject)));
        }
    }
}
