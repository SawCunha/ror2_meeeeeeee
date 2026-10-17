using System;

namespace Ror2Meeeee.Core.Difficulty
{
    // Ganho por eixo: quanto de (Factor - 1) vira multiplicador naquele eixo. Curva linear no MVP.
    public readonly struct DifficultyGains
    {
        public DifficultyGains(float health, float damage, float quantity, float pace)
        {
            Health = health;
            Damage = damage;
            Quantity = quantity;
            Pace = pace;
        }

        public float Health { get; }
        public float Damage { get; }
        public float Quantity { get; }
        public float Pace { get; }
    }

    public readonly struct DifficultyMultipliers
    {
        public DifficultyMultipliers(float health, float damage, float quantity, float pace)
        {
            Health = health;
            Damage = damage;
            Quantity = quantity;
            Pace = pace;
        }

        public static readonly DifficultyMultipliers Identity = new DifficultyMultipliers(1f, 1f, 1f, 1f);

        public float Health { get; }
        public float Damage { get; }
        public float Quantity { get; }
        public float Pace { get; }
    }

    public static class DifficultyCurve
    {
        public const float DefaultFactor = 1f;
        public const float MinMultiplier = 0.1f;
        public const float MaxMultiplier = 1000f;

        public static float SanitizeFactor(float factor)
        {
            if (float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f) return DefaultFactor;
            return factor;
        }

        // Única função da curva: trocar a curva = mexer só aqui.
        public static DifficultyMultipliers Evaluate(float factor, DifficultyGains gains)
        {
            var f = SanitizeFactor(factor);
            return new DifficultyMultipliers(
                Axis(f, gains.Health),
                Axis(f, gains.Damage),
                Axis(f, gains.Quantity),
                Axis(f, gains.Pace));
        }

        // Bônus fixo em % por cima do multiplicador do eixo (50 = +50%). NaN/Inf = sem bônus.
        public static float WithBonus(float multiplier, float bonusPercent)
        {
            if (float.IsNaN(bonusPercent) || float.IsInfinity(bonusPercent)) bonusPercent = 0f;
            var m = (double)multiplier * (1.0 + bonusPercent / 100.0);
            return (float)Math.Min(MaxMultiplier, Math.Max(MinMultiplier, m));
        }

        private static float Axis(float factor, float gain)
        {
            if (float.IsNaN(gain) || float.IsInfinity(gain)) return 1f;
            var m = 1.0 + ((double)factor - 1.0) * gain;
            return (float)Math.Min(MaxMultiplier, Math.Max(MinMultiplier, m));
        }
    }

    // Efeito do item "Chamado da Horda": multiplicador de quantidade por stack, saturado em max.
    public static class HordeCurve
    {
        public static float QuantityMultiplier(int stacks, float perStack, float max)
        {
            if (stacks <= 0 || float.IsNaN(perStack) || float.IsNaN(max) || perStack <= 0f || max < 1f) return 1f;
            var m = 1.0 + (double)stacks * perStack;
            return (float)Math.Min(max, m);
        }
    }
}
