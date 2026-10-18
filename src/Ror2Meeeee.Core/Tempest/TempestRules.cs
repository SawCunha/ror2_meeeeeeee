using System;
using System.Collections.Generic;

namespace Ror2Meeeee.Core.Tempest
{
    // Curva de um eixo da Tempestade: 1 + PerMinute * minutos + PerStage * estágios, entre 1 e Max. Sem termo de loop.
    public readonly struct TempestCurve
    {
        public TempestCurve(float perMinute, float perStage, float max)
        {
            PerMinute = perMinute;
            PerStage = perStage;
            Max = max;
        }

        public float PerMinute { get; }
        public float PerStage { get; }
        public float Max { get; }

        public float Evaluate(float minutes, int stagesCleared)
        {
            var v = 1.0 + (double)PerMinute * Math.Max(0f, minutes) + (double)PerStage * Math.Max(0, stagesCleared);
            return (float)Math.Min(Math.Max(1f, Max), Math.Max(1.0, v));
        }
    }

    // Regras puras da dificuldade Tempestade (§7 e §8 da especificação). Sem referência ao jogo.
    public static class TempestRules
    {
        public const int MinCapacity = 40;       // softCharacterLimit do time Monster no jogo
        public const int MaxReductionPercent = 99;
        public const int MaxSteps = 20;
        public static readonly int[] DefaultSteps = { 20, 40, 55, 70, 90, 99 };

        // Valor fora da faixa vai para o limite mais próximo; NaN/Inf vão para o padrão.
        public static float Clamp(float value, float defaultValue, float min, float max, out bool corrected)
        {
            var v = float.IsNaN(value) || float.IsInfinity(value) ? defaultValue : Math.Min(max, Math.Max(min, value));
            corrected = v != value;
            return v;
        }

        public static int Clamp(int value, int min, int max, out bool corrected)
        {
            var v = Math.Min(max, Math.Max(min, value));
            corrected = v != value;
            return v;
        }

        // "20,40,55,70,90,99": % por estágio concluído. Item não numérico = lista padrão inteira; valores fora de
        // 0..99 cortados; lista forçada a não decrescer; no máximo MaxSteps itens. Vazia = sem redução.
        public static int[] ParseSteps(string text, out bool corrected)
        {
            corrected = false;
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<int>();
            var parts = text.Split(',');
            var list = new List<int>(parts.Length);
            var previous = 0;
            foreach (var raw in parts)
            {
                var p = raw.Trim();
                if (p.Length == 0) { corrected = true; continue; }
                if (!int.TryParse(p, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    corrected = true;
                    return (int[])DefaultSteps.Clone();
                }
                var c = Math.Min(MaxReductionPercent, Math.Max(previous, v));
                if (c != v) corrected = true;
                previous = c;
                if (list.Count == MaxSteps) { corrected = true; break; }
                list.Add(c);
            }
            return list.ToArray();
        }

        // Redução de dano (= bônus de vida) do estágio, 0..0,99. Estágio 1 (0 concluídos) = 0; depois o último valor se repete.
        public static float Reduction(int stagesCleared, int[] steps)
        {
            if (stagesCleared <= 0 || steps == null || steps.Length == 0) return 0f;
            return steps[Math.Min(stagesCleared, steps.Length) - 1] / 100f;
        }

        // Com o Multiplicador de itens (x M), o dano que passa é dividido por M^expoente: x10 com 0,5 leva 20% a 75%.
        // Estágio 1 continua 0 (sem redução base); teto MaxReductionPercent. M <= 1 ou expoente <= 0 = sem mudança.
        public static float Reduction(int stagesCleared, int[] steps, float multiplier, float exponent)
        {
            var r = Reduction(stagesCleared, steps);
            if (stagesCleared <= 0 || multiplier <= 1f || exponent <= 0f || float.IsNaN(multiplier) || float.IsNaN(exponent)) return r;
            var passes = (1.0 - r) / MultiplierFactor(multiplier, exponent);
            return (float)Math.Min(MaxReductionPercent / 100.0, 1.0 - passes);
        }

        // Reforço pelo Multiplicador de itens: M^expoente (x10 com 0,5 = 3,16). M <= 1 ou expoente <= 0 = 1 (sem reforço).
        public static float MultiplierFactor(float multiplier, float exponent) =>
            multiplier <= 1f || exponent <= 0f || float.IsNaN(multiplier) || float.IsNaN(exponent) ? 1f : (float)Math.Pow(multiplier, exponent);

        // Dio's Best Friend de cada inimigo: chefe nenhum (mesmo elite), elite eliteLives, os outros normalLives.
        public static int ExtraLives(bool boss, bool elite, int normalLives, int eliteLives) =>
            boss ? 0 : Math.Max(0, elite ? eliteLives : normalLives);

        // Stacks do item oculto de vida (+1% cada).
        public static int HealthStacks(float reduction) => (int)Math.Round(Math.Min(0.99, Math.Max(0.0, reduction)) * 100.0, MidpointRounding.AwayFromZero);

        // Stacks de BoostHp/BoostDamage (+10% cada) para um multiplicador de eixo.
        public static int BoostStacks(float multiplier) =>
            float.IsNaN(multiplier) || multiplier <= 1f ? 0 : (int)Math.Min(int.MaxValue, Math.Round((multiplier - 1.0) * 10.0, MidpointRounding.AwayFromZero));

        // Elite mais barato: eliteBias = 1 / multiplicador do eixo de elites (>= 1).
        public static float EliteBias(float eliteMultiplier) =>
            float.IsNaN(eliteMultiplier) || eliteMultiplier <= 1f ? 1f : 1f / eliteMultiplier;

        public static int Capacity(int players, int baseCapacity, int perExtraPlayer, int maxCapacity)
        {
            var max = Math.Max(MinCapacity, maxCapacity);
            var v = (long)baseCapacity + (long)Math.Max(0, perExtraPlayer) * (Math.Max(1, players) - 1);
            return (int)Math.Min(max, Math.Max(MinCapacity, v));
        }

        // Portão do crédito do mod: fecha (0) quando as vagas livres acabam; reabre em rampa de rampSeconds e nunca
        // passa de livres / folga (perto do teto, menos crédito).
        public static float Gate(float previous, int capacity, int alive, int headroom, float rampSeconds, float dt)
        {
            var free = capacity - Math.Max(1, headroom) - alive;
            if (free <= 0) return 0f;
            var ramp = rampSeconds > 0f ? previous + dt / rampSeconds : 1f;
            return Math.Min(1f, Math.Min(ramp, (float)free / Math.Max(1, headroom)));
        }

        // Pressão por permanência: soma à curva de spawn depois de startSeconds no estágio (sem contar a carga do teleportador).
        public static float Pressure(float stageSeconds, float startSeconds, float perMinute, float max)
        {
            if (stageSeconds <= startSeconds || perMinute <= 0f || max <= 0f) return 0f;
            return Math.Min(max, (stageSeconds - startSeconds) / 60f * perMinute);
        }

        // Crédito extra do mod por segundo. Mesma forma do DirectorMoneyWave do jogo:
        // (1 + 0,4 * coeficiente) * (0,5 + 0,5 * jogadores). spawn = curva de spawn + pressão (1 = nada extra).
        public static float Credit(float perSecond, float difficultyCoefficient, int players, float spawn, float gate, float perf)
        {
            if (perSecond <= 0f || spawn <= 1f || gate <= 0f || perf <= 0f) return 0f;
            var b = perSecond * (1f + 0.4f * Math.Max(0f, difficultyCoefficient)) * (0.5f + 0.5f * Math.Max(1, players));
            return b * (spawn - 1f) * Math.Min(1f, gate) * Math.Min(1f, perf);
        }

        // Fase de enxame (aceita cartas baratas) no começo de cada ciclo do estágio; swarmSeconds = 0 desliga.
        public static bool IsSwarm(float stageSeconds, float swarmSeconds, float heavySeconds)
        {
            if (swarmSeconds <= 0f) return false;
            var cycle = swarmSeconds + Math.Max(0f, heavySeconds);
            if (stageSeconds < 0f) stageSeconds = 0f;
            return stageSeconds % cycle < swarmSeconds;
        }

        // FinalStage aceita só múltiplos de 5 (estágios com Teleportador Primordial); mínimo 5.
        public static int SanitizeFinalStage(int finalStage, out bool corrected)
        {
            var v = Math.Max(5, (int)Math.Round(finalStage / 5.0, MidpointRounding.AwayFromZero) * 5);
            corrected = v != finalStage;
            return v;
        }

        // Saída para o final ainda fechada neste estágio? (stageClearCount 9 = estágio 10.)
        public static bool FinalLocked(int stagesCleared, int finalStage) => stagesCleared + 1 < finalStage;
    }
}
