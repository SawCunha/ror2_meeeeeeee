using System;
using RoR2;

namespace Ror2Meeeee
{
    // Um único assinante de PurchaseInteraction.onPurchaseGlobalServer, com ordem fixa (antes, cada feature assinava o
    // evento e a ordem dependia de quem ligou primeiro, inclusive ao religar pela tela de mods):
    //   1. Spend: registra o gasto do jogador
    //   2. Taxade: troca de drop do Capitalismo e a taxa em vida, SEMPRE POR ÚLTIMO
    // Registrar/limpar é idempotente; o evento só é assinado enquanto houver um passo.
    internal static class PurchaseHub
    {
        private static Action<CostTypeDef.PayCostContext, CostTypeDef.PayCostResults> _spend, _tax;
        private static bool _hooked;

        public static void SetSpend(Action<CostTypeDef.PayCostContext, CostTypeDef.PayCostResults> f) { _spend = f; Sync(); }
        public static void SetTax(Action<CostTypeDef.PayCostContext, CostTypeDef.PayCostResults> f) { _tax = f; Sync(); }

        private static void Sync()
        {
            var want = _spend != null || _tax != null;
            if (want == _hooked) return;
            if (want) PurchaseInteraction.onPurchaseGlobalServer += OnPurchase;
            else PurchaseInteraction.onPurchaseGlobalServer -= OnPurchase;
            _hooked = want;
        }

        // Cada passo trata as próprias exceções (Feature.Fail); um não impede o outro.
        private static void OnPurchase(CostTypeDef.PayCostContext ctx, CostTypeDef.PayCostResults results)
        {
            _spend?.Invoke(ctx, results);
            _tax?.Invoke(ctx, results);
        }
    }
}
