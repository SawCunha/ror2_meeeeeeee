namespace Ror2Meeeee.Core.Stats
{
    // Item "Cano Extra": cada cópia = +1 tiro na arma principal, em leque ao redor da mira.
    public static class MultiShot
    {
        // Tiros extras para `stacks` cópias, respeitando o teto (teto <= 0 = sem tiro extra).
        public static int ExtraShots(int stacks, int maxExtra)
        {
            if (stacks <= 0 || maxExtra <= 0) return 0;
            return stacks < maxExtra ? stacks : maxExtra;
        }

        // Ângulo (graus, no plano horizontal) do tiro extra `index` (0-based): alterna direita/esquerda e abre
        // a cada par: +step, -step, +2step, -2step... O tiro original fica no centro.
        public static float FanAngle(int index, float step)
        {
            if (index < 0) return 0f;
            var ring = index / 2 + 1;
            return (index % 2 == 0 ? 1f : -1f) * ring * step;
        }

        // Válvula de FPS: quantos extras ainda cabem neste frame.
        public static int Budget(int wanted, int usedThisFrame, int maxPerFrame)
        {
            var left = maxPerFrame - usedThisFrame;
            if (wanted <= 0 || left <= 0) return 0;
            return wanted < left ? wanted : left;
        }
    }
}
