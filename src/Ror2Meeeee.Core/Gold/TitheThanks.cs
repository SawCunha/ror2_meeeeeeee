using System;
using System.Globalization;

namespace Ror2Meeeee.Core.Gold
{
    // Agradecimento do diretor pelo crédito vindo do Artefato do Dízimo: acumula e fala no chat
    // no máximo 1 vez por intervalo (nada por tick).
    public static class TitheThanks
    {
        private static readonly string[] Lines =
        {
            "Obrigado pelo dízimo! R$ {1} de vocês já viraram {0} de crédito em monstro.",
            "Sua generosidade foi notada: R$ {1} viraram +{0} de crédito de spawn.",
            "R$ {1} recolhidos, {0} de crédito investido em companhia para vocês.",
            "O diretor agradece a contribuição de R$ {1}. Os monstros também ({0} de crédito).",
        };

        // Hora de falar? intervalo <= 0 desliga; nada acumulado não fala.
        public static bool Due(double now, double lastPost, double interval, double pendingCredit) =>
            interval > 0 && pendingCredit >= 1 && now - lastPost >= interval;

        // `index` escolhe a frase em rodízio (sem RNG: não mexe na seed da run).
        public static string Format(int index, double credit, ulong gold)
        {
            var inv = CultureInfo.InvariantCulture;
            var line = Lines[(int)((uint)index % (uint)Lines.Length)];
            return "<color=#c792ea>Diretor:</color> " + string.Format(inv, line,
                Thousands(Math.Floor(credit).ToString("N0", inv)), Thousands(gold.ToString("N0", inv)));
        }

        // Milhar no formato brasileiro (12.345), sem depender de CultureInfo "pt-BR" existir no Mono do jogo.
        private static string Thousands(string invariantN0) => invariantN0.Replace(',', '.');
    }
}
