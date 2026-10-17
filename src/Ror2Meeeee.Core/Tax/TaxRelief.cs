namespace Ror2Meeeee.Core.Tax
{
    // Itens "Capitalismo" (branco/verde/vermelho, no máximo 1 de cada por jogador).
    // Alívio do Taxade: 20 + 30 + 50 = 100%, com os três a taxa some.
    // Bônus de ouro: +10% / +20% / +30%, somados (os três = +60%).
    public static class TaxRelief
    {
        public const int WhitePercent = 20;
        public const int GreenPercent = 30;
        public const int RedPercent = 50;

        public const int WhiteGoldPercent = 10;
        public const int GreenGoldPercent = 20;
        public const int RedGoldPercent = 30;

        // Fator aplicado ao dano da taxa (1 = taxa cheia, 0 = anulada). Inteiros: sem erro de ponto flutuante.
        public static float Multiplier(bool white, bool green, bool red)
        {
            var reduced = (white ? WhitePercent : 0) + (green ? GreenPercent : 0) + (red ? RedPercent : 0);
            return (100 - reduced) / 100f;
        }

        // Ouro com o bônus, arredondado (meio para cima) para valer também em quantias pequenas; nunca estoura.
        public static uint Gold(uint amount, bool white, bool green, bool red)
        {
            var bonus = (white ? WhiteGoldPercent : 0) + (green ? GreenGoldPercent : 0) + (red ? RedGoldPercent : 0);
            if (bonus == 0 || amount == 0) return amount;
            var total = ((ulong)amount * (ulong)(100 + bonus) + 50UL) / 100UL;
            return total > uint.MaxValue ? uint.MaxValue : (uint)total;
        }
    }
}
