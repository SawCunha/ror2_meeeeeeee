using System;
using System.Globalization;

namespace Ror2Meeeee.Core.Gold
{
    // Agradecimento do diretor pelo crédito vindo do Artefato do Dízimo: acumula e fala no chat
    // no máximo 1 vez por intervalo (nada por tick). Mesmos argumentos para os dois idiomas (Args):
    // {0}/{1} = crédito/ouro no formato brasileiro, {2}/{3} = no formato inglês.
    public static class TitheThanks
    {
        public const string PrefixPt = "<color=#c792ea>Diretor:</color> ";
        public const string PrefixEn = "<color=#c792ea>Director:</color> ";

        public static readonly string[] LinesPt =
        {
            "Obrigado pelo dízimo! R$ {1} de vocês já viraram {0} de crédito em monstro.",
            "Sua generosidade foi notada: R$ {1} viraram +{0} de crédito de spawn.",
            "R$ {1} recolhidos, {0} de crédito investido em companhia para vocês.",
            "O diretor agradece a contribuição de R$ {1}. Os monstros também ({0} de crédito).",
        };

        public static readonly string[] LinesEn =
        {
            "Thanks for the tithe! Your ${3} has already become {2} credit in monsters.",
            "Your generosity has been noticed: ${3} became +{2} spawn credit.",
            "${3} collected, {2} credit invested in company for you.",
            "The director thanks you for the ${3} contribution. So do the monsters ({2} credit).",
        };

        // Hora de falar? intervalo <= 0 desliga; nada acumulado não fala.
        public static bool Due(double now, double lastPost, double interval, double pendingCredit) =>
            interval > 0 && pendingCredit >= 1 && now - lastPost >= interval;

        // `index` escolhe a frase em rodízio (sem RNG: não mexe na seed da run).
        public static int Line(int index) => (int)((uint)index % (uint)LinesPt.Length);

        public static string[] Args(double credit, ulong gold)
        {
            var inv = CultureInfo.InvariantCulture;
            var c = Math.Floor(credit).ToString("N0", inv);
            var g = gold.ToString("N0", inv);
            return new[] { Thousands(c), Thousands(g), c, g };
        }

        public static string Format(int index, double credit, ulong gold, bool english = false)
        {
            var line = Line(index);
            return english
                ? PrefixEn + string.Format(CultureInfo.InvariantCulture, LinesEn[line], Args(credit, gold))
                : PrefixPt + string.Format(CultureInfo.InvariantCulture, LinesPt[line], Args(credit, gold));
        }

        // Milhar no formato brasileiro (12.345), sem depender de CultureInfo "pt-BR" existir no Mono do jogo.
        private static string Thousands(string invariantN0) => invariantN0.Replace(',', '.');
    }
}
