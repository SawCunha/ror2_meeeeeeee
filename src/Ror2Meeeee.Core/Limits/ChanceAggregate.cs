using System;

namespace Ror2Meeeee.Core.Limits
{
    // Itens de chance linear por stack (ex.: Ghor's Tome, 4%/stack, teto 100%): com o stack limitado
    // a chance cai; o prêmio de cada acerto é multiplicado por este fator para manter o valor esperado.
    public static class ChanceAggregate
    {
        public static float Factor(int stack, int capped, float chancePerStack)
        {
            if (capped <= 0 || stack <= capped || chancePerStack <= 0f) return 1f;
            var real = Math.Min(100f, chancePerStack * stack);
            var cap = Math.Min(100f, chancePerStack * capped);
            return real / cap;
        }
    }
}
