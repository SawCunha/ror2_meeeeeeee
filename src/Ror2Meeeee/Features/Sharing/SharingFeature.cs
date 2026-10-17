using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using RoR2;
using Ror2Meeeee.Core.Pipeline;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618 // smallIcon*Sprite: campos obsoletos no 1.5.0, mas ainda lidos pelo jogo

namespace Ror2Meeeee
{
    // Artefato do Comunismo: com ele ligado no lobby, todo item pego (todos os tiers, de todas as expansões, sucata,
    // temporários e alugados) é replicado aos demais jogadores vivos, só no host (v2, Epic 6). Mesmo ponto de
    // interceptação da MultiplierFeature (ItemDef.AttemptGrant), sem referenciá-la: lê as mesmas chaves de config.
    // A concessão usa GiveItemPermanent/Temp/Rented (não passam por AttemptGrant), então não há recursão nem multiplicação dupla.
    public sealed class SharingFeature : Feature
    {
        public const string ArtifactName = "Ror2MeeeeeCommunism";
        public const string Token = "ROR2MEEEEE_ARTIFACT_COMMUNISM";

        private static ArtifactDef _artifact;
        private readonly ConfigEntry<bool> _multiplierEnabled;
        private readonly ConfigEntry<int> _value;
        private readonly ConfigEntry<bool> _includeLunar;
        private readonly ConfigEntry<bool> _includeVoid;
        private readonly ConfigEntry<int> _stackCap;
        private bool _granting;
        // Reaproveitadas a cada pickup (sem reentrância: GiveItem* não passa por AttemptGrant).
        private readonly List<CharacterMaster> _others = new List<CharacterMaster>();
        private readonly List<Recipient> _recipients = new List<Recipient>();

        // Cria a feature sem opção de liga/desliga (quem controla é o artefato) e liga as entradas de config do
        // Multiplicador (mesmas chaves, mesma instância no BepInEx).
        public SharingFeature(Cfg cfg, ManualLogSource log)
            : base("Sharing", cfg, log, (ConfigEntry<bool>)null)
        {
            // Mesmas definições da MultiplierFeature: o BepInEx devolve a mesma entrada.
            _multiplierEnabled = cfg.BindEnabled("Multiplier");
            _value = cfg.Bind("Multiplier", "Value", MultiplierRules.DefaultMultiplier,
                $"Quantas cópias no total cada item coletado rende (1 = normal, 2 = dobra, 10 = dez cópias). Menor que 1 volta para {MultiplierRules.DefaultMultiplier}.");
            _includeLunar = cfg.Bind("Multiplier", "IncludeLunar", true, "Também multiplica itens lunares (azuis). Desligado: lunares vêm em 1 cópia.");
            _includeVoid = cfg.Bind("Multiplier", "IncludeVoid", true, "Também multiplica itens void (roxos). Desligado: void vêm em 1 cópia.");
            _stackCap = cfg.Bind("Multiplier", "StackCap", 150, "Teto de cópias de um mesmo item por jogador (qualquer item). A multiplicação e o compartilhamento param no teto; item pego com o stack já no teto some (aviso no chat). Impressora, caldeirão e give_item não passam por aqui. 0 = sem limite.");
        }

        // Chamar no Awake do plugin (antes de o ArtifactCatalog existir).
        public static void Register(ManualLogSource log)
        {
            try
            {
                _artifact = ScriptableObject.CreateInstance<ArtifactDef>();
                _artifact.cachedName = ArtifactName;
                _artifact.nameToken = Token + "_NAME";
                _artifact.descriptionToken = Token + "_DESC";
                _artifact.smallIconSelectedSprite = TitheArtifact.LoadIcon("comunismo_selecionado.png", new Color32(200, 20, 20, 255));
                _artifact.smallIconDeselectedSprite = TitheArtifact.LoadIcon("comunismo_desmarcado.png", new Color32(90, 90, 90, 255));

                LanguageAPI.Add(Token + "_NAME", "Artefato do Comunismo");
                LanguageAPI.Add(Token + "_DESC", "Todo item pego por um jogador também vai para os outros jogadores vivos. Todos os tipos, de todas as expansões.");

                if (!ContentAddition.AddArtifactDef(_artifact)) { log.LogError("Sharing: ContentAddition recusou o artefato."); _artifact = null; }
                else log.LogInfo($"Sharing: artefato '{ArtifactName}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Sharing: falha ao registrar o artefato (o plugin segue sem ele): {e}");
                _artifact = null;
            }
        }

        // Artefato do Comunismo ligado na run atual? (artifactIndex só existe depois do ArtifactCatalog.)
        private static bool ArtifactOn()
        {
            var mgr = RunArtifactManager.instance;
            return _artifact && mgr && mgr.IsArtifactEnabled(_artifact);
        }

        // Limpa o estado entre runs: trava de reentrância e listas que podem segurar masters antigos.
        public override void ResetRunState()
        {
            _granting = false;
            _others.Clear(); // não segurar masters da run anterior
            _recipients.Clear();
        }

        // Registra o hook em ItemDef.AttemptGrant, por onde passa todo pickup de item.
        protected override void OnEnable()
        {
            Track("ItemDef.AttemptGrant", () => On.RoR2.ItemDef.AttemptGrant += OnAttemptGrant);
        }

        // Remove o hook e solta a trava de reentrância.
        protected override void OnDisable()
        {
            On.RoR2.ItemDef.AttemptGrant -= OnAttemptGrant;
            _granting = false;
        }

        // Hook do pickup: deixa o jogo conceder o item ao dono (orig) e, com o artefato ativo no host, concede as
        // cópias calculadas pelo SharingPipeline aos demais jogadores, no mesmo modo (permanente, temporário ou alugado).
        private void OnAttemptGrant(On.RoR2.ItemDef.orig_AttemptGrant orig, ref PickupDef.GrantContext context)
        {
            if (_granting || !NetworkServer.active || !ArtifactOn())
            {
                orig(ref context);
                return;
            }

            CharacterBody body = null;
            ItemDef itemDef = null;
            var kind = Core.Eligibility.ItemKind.Other;
            var rental = false;
            var tempDecay = 0f; // > 0: pickup temporário
            try
            {
                body = context.body;
                var pickup = context.pickup;
                rental = pickup.isRental;
                if (pickup.isTempItem) tempDecay = pickup.decayValue;
                var def = PickupCatalog.GetPickupDef(pickup.pickupIndex);
                itemDef = def != null ? ItemCatalog.GetItemDef(def.itemIndex) : null;
                if (body && body.master && itemDef)
                    kind = ItemClassifier.Classify(itemDef, pickup.isTempItem || pickup.isRental);
                else itemDef = null;
                if (itemDef && DroppedGrants.IsDropped(itemDef.itemIndex)) itemDef = null; // item solto pelo Drop: não compartilha de novo
            }
            catch (Exception e)
            {
                itemDef = null;
                Fail(e);
            }

            orig(ref context);

            if (!itemDef) return;
            try
            {
                var multiplier = _multiplierEnabled.Value ? _value.Value : 1;
                var rules = new MultiplierRules(multiplier, _includeLunar.Value, _includeVoid.Value, _stackCap.Value);
                var owner = body.master;
                var others = _others;
                var recipients = _recipients;
                others.Clear();
                recipients.Clear();
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    var m = pcmc ? pcmc.master : null;
                    if (!m || m == owner || !m.inventory) continue;
                    var b = m.GetBody();
                    others.Add(m);
                    recipients.Add(new Recipient(m.inventory.GetItemCountPermanent(itemDef.itemIndex),
                        b && b.healthComponent && b.healthComponent.alive, pcmc.isConnected));
                }
                if (others.Count == 0) return;

                var shares = SharingPipeline.Resolve(
                    new PickupContext(kind, 1, 0, true, true, false), rules, true, false, recipients);

                _granting = true;
                try
                {
                    // Mesmo modo de concessão do original (AttemptGrant): alugado, temporário ou permanente.
                    for (var i = 0; i < shares.Length; i++)
                    {
                        if (shares[i] <= 0) continue;
                        var inv = others[i].inventory;
                        if (rental) inv.GiveItemRented(itemDef.itemIndex, shares[i]);
                        else if (tempDecay > 0f) inv.GiveItemTemp(itemDef.itemIndex, tempDecay * shares[i]);
                        else inv.GiveItemPermanent(itemDef.itemIndex, shares[i]);
                    }
                }
                finally { _granting = false; }
                Log.LogInfo($"[Sharing] {itemDef.name} ({kind}): {others.Count} jogador(es) de {owner.name}, +{(shares.Length > 0 ? shares[0] : 0)} cada");
            }
            catch (Exception e)
            {
                _granting = false;
                Fail(e);
            }
        }
    }
}
