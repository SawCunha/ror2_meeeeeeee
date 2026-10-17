using System;
using System.Globalization;
using System.Text;

namespace Ror2Meeeee.Core.Progression
{
    public readonly struct LadderRules
    {
        public LadderRules(float raiseAt, float step, float maxMultiplier)
        {
            RaiseAt = raiseAt;
            Step = step;
            MaxMultiplier = maxMultiplier;
        }

        public float RaiseAt { get; }        // nota mínima (com vitória) para subir um nível
        public float Step { get; }           // orçamento extra do diretor por nível (0.15 = +15%)
        public float MaxMultiplier { get; }  // teto do multiplicador (contra-métrica: não deixar de ser divertido)
    }

    // Nível de dificuldade entre runs: vitória com nota boa sobe, derrota desce, o resto mantém.
    public static class ProgressionLadder
    {
        public static int Next(int level, float score, bool won, LadderRules r)
        {
            level = Math.Max(0, level);
            var next = !won ? level - 1
                : !float.IsNaN(score) && score >= r.RaiseAt ? level + 1
                : level;
            return Math.Max(0, Math.Min(next, MaxLevel(r)));
        }

        // Nível a partir do qual o multiplicador satura: subir além disso não muda nada.
        public static int MaxLevel(LadderRules r)
        {
            if (!ValidStep(r.Step) || !(r.MaxMultiplier > 1f) || float.IsInfinity(r.MaxMultiplier)) return 0;
            return (int)Math.Min(10000, Math.Ceiling((r.MaxMultiplier - 1.0) / r.Step - 1e-6));
        }

        public static float Multiplier(int level, LadderRules r)
        {
            if (level <= 0 || !ValidStep(r.Step) || !(r.MaxMultiplier > 1f)) return 1f;
            return (float)Math.Min(r.MaxMultiplier, 1.0 + (double)level * r.Step);
        }

        private static bool ValidStep(float step) => !float.IsNaN(step) && !float.IsInfinity(step) && step > 0f;

        // Arquivo de estado: "level=3\nruns=7\n". Ilegível ou ausente = nível 0.
        public static string Serialize(int level, int runs) =>
            "level=" + Math.Max(0, level).ToString(CultureInfo.InvariantCulture) + "\n"
            + "runs=" + Math.Max(0, runs).ToString(CultureInfo.InvariantCulture) + "\n";

        public static void Parse(string text, out int level, out int runs)
        {
            level = 0;
            runs = 0;
            if (string.IsNullOrEmpty(text)) return;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                if (!int.TryParse(line.Substring(eq + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) || v < 0) continue;
                switch (line.Substring(0, eq).Trim())
                {
                    case "level": level = v; break;
                    case "runs": runs = v; break;
                }
            }
        }

        // Resumo curto para o chat (poucas linhas; o detalhe vai para o CSV).
        public static string Summary(RunStats s, RunScoreResult score, int oldLevel, int newLevel, float newMultiplier)
        {
            var inv = CultureInfo.InvariantCulture;
            var minutes = s.Seconds / 60.0;
            var sb = new StringBuilder();
            sb.Append("<color=#e5c07b>Ror2Meeeee</color> ").Append(s.Won ? "vitória" : "derrota")
              .Append(" em ").Append(minutes.ToString("F1", inv)).Append(" min | nota ")
              .Append((score.Total * 100f).ToString("F0", inv)).Append("/100\n");
            sb.Append("velocidade ").Append(Pct(score.Speed)).Append(" · sobrevivência ").Append(Pct(score.Survival))
              .Append(" · dano sofrido ").Append(Pct(score.Damage)).Append(" · abates ").Append(Pct(score.Kills)).Append('\n');
            sb.Append(s.Kills).Append(" abates, ").Append(s.Deaths).Append(" mortes, ")
              .Append(s.HealthBarsLost.ToString("F1", inv)).Append(" vidas de dano sofrido\n");
            var arrow = newLevel > oldLevel ? "sobe" : newLevel < oldLevel ? "desce" : "mantém";
            sb.Append("Diretor ").Append(arrow).Append(": nível ").Append(oldLevel).Append(" -> ").Append(newLevel)
              .Append(" (orçamento de spawn x").Append(newMultiplier.ToString("F2", inv)).Append(" na próxima run)");
            return sb.ToString();
        }

        private static string Pct(float v) => (v * 100f).ToString("F0", CultureInfo.InvariantCulture);
    }
}
