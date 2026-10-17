using System;

namespace Ror2Meeeee.Core.Gold
{
    // Crédito de spawn do diretor gerado por ouro ganho sob o Artefato do Dízimo.
    public static class GoldCredit
    {
        // amount * stacks * ratioPerStack; sem item, ouro 0 ou razão inválida = 0 (nunca negativo/NaN).
        public static float Compute(uint amount, int stacks, float ratioPerStack)
        {
            if (amount == 0 || stacks <= 0 || float.IsNaN(ratioPerStack) || float.IsInfinity(ratioPerStack) || ratioPerStack <= 0f)
                return 0f;
            return (float)Math.Min((double)amount * stacks * ratioPerStack, float.MaxValue);
        }
    }
}
