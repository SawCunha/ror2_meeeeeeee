using System;

namespace Ror2Meeeee.Core.Stats
{
    // Item lunar "Coração Pesado": cada cópia MULTIPLICA a vida máxima por 1,5 e o dano recebido por 1,3 (composto, sem
    // teto de balanço). A vantagem (vida efetiva) cresce ~1,15x por cópia.
    public static class HeavyHeart
    {
        public const float HealthPerStack = 0.5f;       // x1,5 por cópia
        public const float DamageTakenPerStack = 0.3f;  // x1,3 por cópia
        // Só proteção numérica: 1,5^n estoura o float (~3,4e38) perto de 219 cópias e viraria Infinity/NaN na vida.
        public const double MaxMultiplier = 1e30;

        public static float HealthMultiplier(int stacks) => Compound(1.0 + HealthPerStack, stacks);

        public static float DamageTakenMultiplier(int stacks) => Compound(1.0 + DamageTakenPerStack, stacks);

        private static float Compound(double factor, int stacks)
        {
            if (stacks <= 0) return 1f;
            var m = Math.Pow(factor, stacks);
            return (float)(m < MaxMultiplier ? m : MaxMultiplier);
        }

    }
}
