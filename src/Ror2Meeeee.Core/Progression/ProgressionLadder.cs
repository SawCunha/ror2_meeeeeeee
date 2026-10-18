using System;
using System.Globalization;

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
        // Resumo do fim da run: modelo por idioma + os mesmos argumentos (SummaryArgs); as palavras que mudam com o
        // resultado vão nos dois idiomas ({0}/{1} resultado, {11}/{12} direção) para o cliente montar a frase no seu.
        public const string SummaryPt =
            "<color=#e5c07b>Ror2Meeeee</color> {0} em {2} min | nota {3}/100\n" +
            "velocidade {4} · sobrevivência {5} · dano sofrido {6} · abates {7}\n" +
            "{8} abates, {9} mortes, {10} vidas de dano sofrido\n" +
            "Diretor {11}: nível {13} -> {14} (orçamento de spawn x{15} na próxima run)";

        public const string SummaryEn =
            "<color=#e5c07b>Ror2Meeeee</color> {1} in {2} min | score {3}/100\n" +
            "speed {4} · survival {5} · damage taken {6} · kills {7}\n" +
            "{8} kills, {9} deaths, {10} health bars of damage taken\n" +
            "Director {12}: level {13} -> {14} (spawn budget x{15} next run)";

        public static string[] SummaryArgs(RunStats s, RunScoreResult score, int oldLevel, int newLevel, float newMultiplier)
        {
            var inv = CultureInfo.InvariantCulture;
            return new[]
            {
                s.Won ? "vitória" : "derrota", s.Won ? "victory" : "defeat",
                (s.Seconds / 60.0).ToString("F1", inv), (score.Total * 100f).ToString("F0", inv),
                Pct(score.Speed), Pct(score.Survival), Pct(score.Damage), Pct(score.Kills),
                s.Kills.ToString(inv), s.Deaths.ToString(inv), s.HealthBarsLost.ToString("F1", inv),
                newLevel > oldLevel ? "sobe" : newLevel < oldLevel ? "desce" : "mantém",
                newLevel > oldLevel ? "goes up" : newLevel < oldLevel ? "goes down" : "holds",
                oldLevel.ToString(inv), newLevel.ToString(inv), newMultiplier.ToString("F2", inv),
            };
        }

        public static string Summary(RunStats s, RunScoreResult score, int oldLevel, int newLevel, float newMultiplier, bool english = false) =>
            string.Format(CultureInfo.InvariantCulture, english ? SummaryEn : SummaryPt, SummaryArgs(s, score, oldLevel, newLevel, newMultiplier));

        private static string Pct(float v) => (v * 100f).ToString("F0", CultureInfo.InvariantCulture);
    }
}
