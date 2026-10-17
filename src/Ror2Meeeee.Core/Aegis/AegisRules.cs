namespace Ror2Meeeee.Core.Aegis
{
    // Égide Vampírica (verde) e Égide do Devorador (void). Valores do design, com teto por causa do multiplicador
    // (1 pickup = várias cópias).
    public static class AegisRules
    {
        public const float BlockChancePercent = 5f;   // chance própria de bloquear (não acumula com cópias)

        public const float StealPerStack = 0.08f;     // verde: % da vida ATUAL do agressor por cópia
        public const float StealCap = 0.5f;

        public const float VoidPerStack = 0.03f;      // void: % da vida atual de cada inimigo próximo por cópia
        public const float VoidCap = 0.2f;
        public const float VoidBaseRadius = 13f;
        public const float VoidRadiusPerStack = 2f;   // por cópia extra
        public const float VoidRadiusCap = 30f;

        public static float StealFraction(int stacks) => Capped(StealPerStack * stacks, stacks, StealCap);

        public static float VoidFraction(int stacks) => Capped(VoidPerStack * stacks, stacks, VoidCap);

        public static float VoidRadius(int stacks)
        {
            if (stacks <= 0) return 0f;
            var r = VoidBaseRadius + VoidRadiusPerStack * (stacks - 1);
            return r < VoidRadiusCap ? r : VoidRadiusCap;
        }

        private static float Capped(float value, int stacks, float cap) => stacks <= 0 ? 0f : value < cap ? value : cap;
    }
}
