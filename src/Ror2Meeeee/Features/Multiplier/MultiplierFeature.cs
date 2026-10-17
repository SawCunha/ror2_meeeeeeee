using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Eligibility;
using Ror2Meeeee.Core.Pipeline;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Multiplica o pickup de itens pelo jogador, só no host. Ponto de interceptação (relatório 1.3):
    // ItemDef.AttemptGrant: só pickup real; itens iniciais, conversões e itens de monstros não passam por aqui.
    public sealed class MultiplierFeature : Feature
    {
        private readonly LimitedCalcFeature _limited;
        private readonly ConfigEntry<int> _value;
        private readonly ConfigEntry<bool> _includeLunar;
        private readonly ConfigEntry<bool> _includeVoid;
        private readonly ConfigEntry<int> _stackCap;
        private MultiplierRules _rules;
        private readonly HashSet<ItemIndex> _loggedSkips = new HashSet<ItemIndex>(); // diagnóstico: 1 linha por item
        private long _logged; // concessões logadas na run (amostragem)
        private bool _granting; // guarda de reentrância: a concessão extra nunca multiplica de novo

        public MultiplierFeature(Cfg cfg, ManualLogSource log, LimitedCalcFeature limited) : base("Multiplier", cfg, log,
            "Multiplicador de itens: cada item que um jogador pega rende várias cópias (veja Value). Itens iniciais, de monstros, scrap e temporários não multiplicam. Só o host precisa ligar.",
            enabledByDefault: true)
        {
            _limited = limited;
            _value = cfg.Bind("Multiplier", "Value", MultiplierRules.DefaultMultiplier,
                $"Quantas cópias no total cada item coletado rende (1 = normal, 2 = dobra, 10 = dez cópias). Menor que 1 volta para {MultiplierRules.DefaultMultiplier}.");
            _includeLunar = cfg.Bind("Multiplier", "IncludeLunar", true, "Também multiplica itens lunares (azuis). Desligado: lunares vêm em 1 cópia.");
            _includeVoid = cfg.Bind("Multiplier", "IncludeVoid", true, "Também multiplica itens void (roxos). Desligado: void vêm em 1 cópia.");
            _stackCap = cfg.Bind("Multiplier", "StackCap", 0, "Máximo de cópias de um mesmo item por jogador depois de multiplicar (0 = sem limite). Itens com Cálculo limitado ligado ignoram este limite.");

            _value.SettingChanged += (_, __) => Refresh();
            _includeLunar.SettingChanged += (_, __) => Refresh();
            _includeVoid.SettingChanged += (_, __) => Refresh();
            _stackCap.SettingChanged += (_, __) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            if (_value.Value < 1)
            {
                Log.LogWarning($"Multiplier.Value={_value.Value} inválido (mínimo 1); usando {MultiplierRules.DefaultMultiplier}.");
                _value.Value = MultiplierRules.DefaultMultiplier; // dispara SettingChanged; o Refresh aninhado já vê o valor válido
                return;
            }
            _rules = new MultiplierRules(_value.Value, _includeLunar.Value, _includeVoid.Value, _stackCap.Value);
            Log.LogInfo($"[Multiplier] Value={_rules.Multiplier}, IncludeLunar={_rules.IncludeLunar}, IncludeVoid={_rules.IncludeVoid}, StackCap={_rules.StackCap}");
        }

        public override void ResetRunState() { _granting = false; _logged = 0; }

        protected override void OnEnable()
        {
            Track("ItemDef.AttemptGrant", () => On.RoR2.ItemDef.AttemptGrant += OnAttemptGrant);
        }

        protected override void OnDisable()
        {
            On.RoR2.ItemDef.AttemptGrant -= OnAttemptGrant;
            _granting = false;
        }

        private void OnAttemptGrant(On.RoR2.ItemDef.orig_AttemptGrant orig, ref PickupDef.GrantContext context)
        {
            // Nada multiplicável com valor 1, no cliente ou durante a nossa própria concessão: só repassa.
            if (_rules.Multiplier <= 1 || _granting || !NetworkServer.active)
            {
                orig(ref context);
                return;
            }

            // Captura o estado ANTES do pickup (o jogo muda o inventário em orig).
            CharacterBody body = null;
            ItemDef itemDef = null;
            var kind = ItemKind.Other;
            var stackBefore = 0;
            var hasContext = false;
            var dropped = false;
            try
            {
                body = context.body;
                var pickup = context.pickup;
                var def = PickupCatalog.GetPickupDef(pickup.pickupIndex);
                itemDef = def != null ? ItemCatalog.GetItemDef(def.itemIndex) : null;
                if (body && body.inventory && itemDef)
                {
                    dropped = DroppedGrants.IsDropped(itemDef.itemIndex); // item solto pelo Drop: pegar de volta não multiplica
                    kind = ItemClassifier.Classify(itemDef, pickup.isTempItem || pickup.isRental);
                    stackBefore = body.inventory.GetItemCountPermanent(itemDef.itemIndex);
                    hasContext = true;
                    if ((kind == ItemKind.Other || kind == ItemKind.Special || kind == ItemKind.Scrap || kind == ItemKind.Temporary) && _loggedSkips.Add(itemDef.itemIndex))
                        Log.LogInfo(ItemClassifier.IsUnique(itemDef.itemIndex)
                            ? $"[Multiplier] {itemDef.name} NÃO multiplicado: item do mod que não multiplica"
                            : $"[Multiplier] {itemDef.name} NÃO multiplicado: tipo {kind}, tier {itemDef.tier} (sem regra de multiplicação)");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }

            orig(ref context);

            if (!hasContext || dropped) return;
            try
            {
                var master = body.master;
                var pcmc = master ? master.playerCharacterMasterController : null;
                var alive = body.healthComponent && body.healthComponent.alive;
                var connected = pcmc && pcmc.isConnected;
                var exempt = _limited.Covers(itemDef);

                var extra = MultiplierPipeline.Resolve(
                    new PickupContext(kind, 1, stackBefore, alive, connected, exempt), _rules);
                if (extra <= 0) return;

                _granting = true;
                try { body.inventory.GiveItemPermanent(itemDef.itemIndex, extra); }
                finally { _granting = false; }
                // Pickups são frequentes: loga os 10 primeiros da run e depois 1 a cada 25.
                if (++_logged <= 10 || _logged % 25 == 0)
                    Log.LogInfo($"[Multiplier] {itemDef.name} ({kind}): +{extra} (stack {stackBefore} -> {stackBefore + 1 + extra}) (#{_logged})");
            }
            catch (Exception e)
            {
                _granting = false;
                Fail(e);
            }
        }
    }
}
