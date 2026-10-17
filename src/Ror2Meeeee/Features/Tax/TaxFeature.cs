using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Com o Artefato Taxade ligado, toda compra paga com ouro custa Tax.Percent da vida máxima. Só no host.
    // Ponto: PurchaseInteraction.onPurchaseGlobalServer (evento do jogo, sem hook de método).
    // Também aplica os itens Capitalismo (com ou sem o artefato): troca no baú o Capitalismo que quem abriu
    // já tem e soma o bônus de ouro em CharacterMaster.GiveMoney.
    public sealed class TaxFeature : Feature
    {
        private readonly ConfigEntry<float> _percent;
        private readonly ConfigEntry<bool> _lethal;
        private float _fraction;
        private bool _lethalValue;
        private long _logged; // compras logadas na run (amostragem)
        private Xoroshiro128Plus _rng; // do mod (não consome RNG da run); criado na 1ª troca, zerado por run

        // Ligada por padrão: os itens Capitalismo existem sempre e só têm efeito com esta feature.
        public TaxFeature(Cfg cfg, ManualLogSource log) : base("Tax", cfg, log,
            "Artefato Taxade e itens Capitalismo: com o artefato ativado no lobby, toda compra paga com ouro (baú, santuário, drone...) custa vida. Os itens Capitalismo (branco, verde, vermelho) reduzem essa taxa e dão bônus de ouro mesmo sem o artefato; esta opção precisa estar ligada para eles funcionarem.",
            enabledByDefault: true)
        {
            _percent = cfg.Bind("Tax", "Percent", 30f, "Artefato Taxade: vida perdida a cada compra paga com ouro, em % da vida máxima (30 = 30%). 0 = sem taxa.");
            _lethal = cfg.Bind("Tax", "Lethal", false, "Desligado: a taxa nunca mata (deixa 1 de vida). Ligado: comprar com pouca vida pode matar.");
            _percent.SettingChanged += (_, __) => Refresh();
            _lethal.SettingChanged += (_, __) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            var p = _percent.Value;
            _fraction = float.IsNaN(p) || float.IsInfinity(p) ? 0f : Mathf.Clamp(p, 0f, 100f) / 100f;
            _lethalValue = _lethal.Value;
            Log.LogInfo($"[Tax] Percent={_fraction * 100f:F0}%, Lethal={_lethalValue}");
        }

        public override void ResetRunState() { _rng = null; _logged = 0; }

        protected override void OnEnable()
        {
            Track("PurchaseInteraction.onPurchaseGlobalServer", () => PurchaseHub.SetTax(OnPurchase)); // sempre o último passo
            Track("CharacterMaster.GiveMoney", () => MoneyHub.SetBefore(BoostMoney));
        }

        protected override void OnDisable()
        {
            PurchaseHub.SetTax(null);
            MoneyHub.SetBefore(null);
        }

        // Frequente (todo ouro de abate, barril, santuário...): 3 consultas de inventário, sem alocação.
        private uint BoostMoney(CharacterMaster self, uint amount)
        {
            try
            {
                if (amount > 0 && NetworkServer.active && self) return TaxReliefItems.BoostGold(self.inventory, amount);
            }
            catch (Exception e)
            {
                Fail(e);
            }
            return amount;
        }

        // O contexto vem de um pool do jogo: nada é guardado depois do callback.
        private void OnPurchase(CostTypeDef.PayCostContext ctx, CostTypeDef.PayCostResults results)
        {
            try
            {
                if (!NetworkServer.active) return;
                var go = ctx.purchasedObject;
                if (!go) return;

                // Capitalismo: só baús soltam item; troca o que quem abriu já tem.
                var chest = go.GetComponent<ChestBehavior>();
                var master = ctx.activatorMaster;
                var inv = master ? master.inventory : null;
                if (chest && inv)
                {
                    var run = Run.instance;
                    if (_rng == null && run) _rng = new Xoroshiro128Plus(run.seed ^ 0x5441584D45454545UL); // "TAXMEEEE"
                    if (_rng != null && TaxReliefItems.TryReplaceOwnedDrop(chest, inv, _rng, out var replaced, out var replacement))
                    {
                        var from = ItemCatalog.GetItemDef(replaced);
                        var to = PickupCatalog.GetPickupDef(replacement); // PickupDef é classe comum, não UnityEngine.Object
                        Log.LogInfo($"[Tax] {Util.GetBestMasterName(master)} já tem {(from ? from.name : "?")}: baú trocado por {(to != null ? to.internalName : "?")}");
                    }
                }

                // Taxa: toda compra paga com ouro (baú, santuário, drone, terminal...); outras moedas não.
                var pi = ctx.purchaseInteraction;
                if (_fraction <= 0f || !pi || pi.costType != CostTypeIndex.Money || !TaxArtifact.IsEnabled()) return;
                var body = ctx.activatorBody;
                var hc = body ? body.healthComponent : null;
                if (!hc || !hc.alive) return;

                var damage = hc.fullHealth * _fraction * TaxReliefItems.TaxMultiplier(body.inventory);
                if (!_lethalValue) damage = Mathf.Min(damage, Mathf.Max(0f, hc.health - 1f));
                if (damage <= 0f) return;

                // NonLethal (sem Lethal): o jogo garante 1 de vida mesmo se algo multiplicar o dano depois deste cálculo
                // (Coração Pesado, Death Mark, outros mods). O Min acima sozinho não bastava.
                var type = DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection;
                if (!_lethalValue) type |= DamageType.NonLethal;
                // Último passo: o valor acima já usa a vida máxima final; o dano passa direto, sem passos do BodyHooks.
                BodyHooks.RawDamage = true;
                try
                {
                    hc.TakeDamage(new DamageInfo
                    {
                        damage = damage,
                        position = body.corePosition,
                        damageType = (DamageTypeCombo)type,
                        damageColorIndex = DamageColorIndex.Bleed,
                    });
                }
                finally { BodyHooks.RawDamage = false; }
                // Compras são frequentes: loga as 5 primeiras da run e depois 1 a cada 50.
                if (++_logged <= 5 || _logged % 50 == 0)
                    Log.LogInfo($"[Tax] {Util.GetBestMasterName(ctx.activatorMaster)} comprou {go.name}: -{damage:F0} vida (#{_logged})");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
