using System;

namespace Ror2Meeeee.Core.Helluva
{
    public static class DropRules
    {
        // Quantos itens soltar: o pedido (mínimo 1) limitado ao que o jogador tem e ao teto por comando.
        public static int ClampCount(int requested, int owned, int max)
        {
            if (owned <= 0 || max <= 0) return 0;
            return Math.Min(Math.Max(requested, 1), Math.Min(owned, max));
        }
    }
}
