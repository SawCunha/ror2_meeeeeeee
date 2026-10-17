using System;

namespace Ror2Meeeee.Core.Progression
{
    // Valor "na média" de cada métrica: nela a nota da métrica é 0.5.
    public readonly struct ScorePars
    {
        public ScorePars(float chargeSeconds, float deathsPerPlayer, float healthBarsPerPlayerMinute, float killsPerMinute)
        {
            ChargeSeconds = chargeSeconds;
            DeathsPerPlayer = deathsPerPlayer;
            HealthBarsPerPlayerMinute = healthBarsPerPlayerMinute;
            KillsPerMinute = killsPerMinute;
        }

        public float ChargeSeconds { get; }
        public float DeathsPerPlayer { get; }
        public float HealthBarsPerPlayerMinute { get; }
        public float KillsPerMinute { get; }
    }

    // Notas de 0 (grupo foi mal) a 1 (grupo dominou). Total = média das quatro.
    public readonly struct RunScoreResult
    {
        public RunScoreResult(float speed, float survival, float damage, float kills)
        {
            Speed = speed;
            Survival = survival;
            Damage = damage;
            Kills = kills;
            Total = (speed + survival + damage + kills) / 4f;
        }

        public float Speed { get; }
        public float Survival { get; }
        public float Damage { get; }
        public float Kills { get; }
        public float Total { get; }
    }

    public static class RunScore
    {
        public const float Neutral = 0.5f; // métrica sem dado (ex.: nenhum teleportador carregado)

        public static RunScoreResult Compute(RunStats s, ScorePars p)
        {
            var players = Math.Max(1, s.Players);
            var minutes = s.Seconds / 60.0;
            var speed = s.ChargedStages > 0 ? Lower(s.ChargeSeconds / s.ChargedStages, p.ChargeSeconds) : Neutral;
            var survival = Lower((double)s.Deaths / players, p.DeathsPerPlayer);
            var damage = minutes > 0 ? Lower(s.HealthBarsLost / players / minutes, p.HealthBarsPerPlayerMinute) : Neutral;
            var kills = minutes > 0 ? Higher(s.Kills / minutes, p.KillsPerMinute) : Neutral;
            return new RunScoreResult(speed, survival, damage, kills);
        }

        // Menor é melhor: 0 -> 1, par -> 0.5, muito acima -> perto de 0.
        internal static float Lower(double value, float par)
        {
            if (!Valid(par) || double.IsNaN(value)) return Neutral;
            return (float)(par / (par + Math.Max(0, value)));
        }

        // Maior é melhor: 0 -> 0, par -> 0.5, muito acima -> perto de 1.
        internal static float Higher(double value, float par)
        {
            if (!Valid(par) || double.IsNaN(value)) return Neutral;
            var v = Math.Max(0, value);
            return (float)(v / (v + par));
        }

        private static bool Valid(float par) => !float.IsNaN(par) && !float.IsInfinity(par) && par > 0f;
    }
}
