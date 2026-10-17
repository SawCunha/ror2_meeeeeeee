namespace Ror2Meeeee.Core.RustyNail
{
    // Pilha de Pregos Enferrujados (branco) e Pilha de Espinhos do Vazio (void, corrompe a primeira). Valores do design.
    // Coeficientes multiplicam o dano base do atacante (CharacterBody.damage).
    public static class RustyNailRules
    {
        public const float ChanceBase = 2f;            // % com 1 cópia
        public const float ChancePerStack = 0.1f;      // branco: % por cópia extra
        public const float VoidChancePerStack = 0.5f;  // void: % por cópia extra
        public const float NailBase = 0.1f;            // prego / espinho com 1 cópia
        public const float NailPerStack = 0.001f;      // branco: +0,1% por cópia extra
        public const float VoidNailPerStack = 0.005f;  // void: +0,5% por cópia extra
        public const float BleedDuration = 3f;         // como o Tri-Tip do jogo (x coeficiente de proc do golpe)

        public const float MarkDuration = 3f;          // Estilhaço do Vazio
        public const float DetonationCoefficient = 2.5f;
        public const float BurstBase = 1.5f;
        public const float BurstPerStack = 0.5f;       // por cópia extra
        public const float BurstRadius = 12f;
        public const int MaxActiveMarks = 200;         // teto global de marcas vivas (protege o FPS em cadeia)

        public static float ChancePercent(int stacks, float perStack)
        {
            if (stacks <= 0) return 0f;
            var c = ChanceBase + perStack * (stacks - 1);
            return c < 100f ? c : 100f;
        }

        public static float NailCoefficient(int stacks, float perStack) => stacks <= 0 ? 0f : NailBase + perStack * (stacks - 1);

        public static float BurstCoefficient(int stacks) => stacks <= 0 ? 0f : BurstBase + BurstPerStack * (stacks - 1);

        // Geração 0 = marca do espinho; cada chuva marca a geração seguinte, até maxChain.
        public static bool CanSpread(int generation, int maxChain) => generation < maxChain;
    }
}
