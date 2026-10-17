using System;
using System.Collections.Generic;

namespace Ror2Meeeee.Core.Bazaar
{
    // Ordem fixa: é o índice das listas de config e de peso.
    public enum BazaarTier { White, Green, Red, Boss, Lunar, Equipment, LunarEquipment }

    // Regras do Bazar Maior (inspirado no BiggerBazaar): sorteio do tier por peso, preço escalado como os baús do jogo
    // e limites em que -1 = sem limite.
    public static class BazaarRules
    {
        public const int TierCount = 7;
        public const int MaxChests = 6; // posições fixas no mapa do bazar
        public const int SmallChestBaseCost = 25; // custo base do baú pequeno no jogo
        public const int Unlimited = -1;

        // Índice escolhido por peso relativo; `roll` em [0, 1). Peso <= 0 ou NaN não concorre. -1 se nenhum concorre.
        public static int PickWeighted(IReadOnlyList<float> weights, double roll)
        {
            double total = 0;
            for (var i = 0; i < weights.Count; i++) total += Valid(weights[i]);
            if (total <= 0) return -1;

            var r = roll * total;
            var last = -1;
            for (var i = 0; i < weights.Count; i++)
            {
                var w = Valid(weights[i]);
                if (w <= 0) continue;
                if (r < w) return i;
                r -= w;
                last = i;
            }
            return last; // arredondamento com roll perto de 1
        }

        // Fórmula do jogo (Run.GetDifficultyScaledCost: base * coef^1.25) vezes o multiplicador, com saturação.
        // Coeficiente inválido vira 1; multiplicador negativo vira 0 (grátis).
        public static int ScaledCost(int baseCost, float difficultyCoefficient, float multiplier)
        {
            double coef = float.IsNaN(difficultyCoefficient) || difficultyCoefficient <= 0 ? 1 : difficultyCoefficient;
            double mult = float.IsNaN(multiplier) || multiplier < 0 ? 0 : multiplier;
            var cost = Math.Max(0, baseCost) * Math.Pow(coef, 1.25) * mult;
            return cost >= int.MaxValue ? int.MaxValue : (int)cost;
        }

        // Preço em ouro de um item: `multiplier` baús pequenos do estágio de referência.
        public static int GoldCost(float difficultyCoefficient, float multiplier) =>
            ScaledCost(SmallChestBaseCost, difficultyCoefficient, multiplier);

        public static bool UnderLimit(int used, int limit) => limit < 0 || used < limit;

        public static int ClampChests(int n) => n < 1 ? 1 : n > MaxChests ? MaxChests : n;

        // Soma com saturação para o ouro do jogador (uint).
        public static uint AddMoney(uint money, long amount)
        {
            var sum = money + amount;
            return sum <= 0 ? 0u : sum >= uint.MaxValue ? uint.MaxValue : (uint)sum;
        }

        private static double Valid(float w) => float.IsNaN(w) || w <= 0 ? 0 : w;
    }
}
