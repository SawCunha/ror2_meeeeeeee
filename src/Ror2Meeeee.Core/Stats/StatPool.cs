namespace Ror2Meeeee.Core.Stats
{
    // Itens que mexem no máximo de vida/escudo DEPOIS do RecalculateStats do jogo: o jogo corta o valor atual quando o
    // máximo que ELE calculou fica abaixo do anterior. O valor guardado antes do cálculo é reaplicado com a regra do jogo.
    public static class StatPool
    {
        // A barra de vida do jogo converte vida+escudo para int (SpriteAsNumberManager): acima de ~2,1 bilhões estoura e
        // lança OverflowException a cada frame. Teto com folga, aplicado no fim dos passos de stats do mod.
        public const float MaxCombined = 1e9f;

        // Fator (<= 1) que traz vida máxima + escudo máximo para dentro do teto, mantendo a proporção entre os dois.
        public static float ClampScale(float maxHealth, float maxShield)
        {
            var total = maxHealth + maxShield;
            return total > MaxCombined && total > 0f ? MaxCombined / total : 1f;
        }

        // Valor atual depois de o máximo mudar de previousMax para newMax: se subiu, ganha a diferença; nunca passa do máximo.
        public static float Adjust(float current, float previousMax, float newMax)
        {
            var gain = newMax - previousMax;
            var v = gain > 0f ? current + gain : current;
            return v < newMax ? v : newMax;
        }
    }
}
