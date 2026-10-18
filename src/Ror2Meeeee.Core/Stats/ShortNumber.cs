using System.Globalization;

namespace Ror2Meeeee.Core.Stats
{
    // Números grandes legíveis na tela de status: pt-BR 950, 12,3 mil, 4,5 mi, 1,2 bi, 3,4 tri; inglês 12.3K, 4.5M, 1.2B, 3.4T.
    public static class ShortNumber
    {
        private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly CultureInfo En = CultureInfo.InvariantCulture;
        private static readonly (double Div, string Pt, string En)[] Steps =
        {
            (1e12, " tri", "T"), (1e9, " bi", "B"), (1e6, " mi", "M"), (1e3, " mil", "K"),
        };

        public static string Format(double value, bool english = false)
        {
            var culture = english ? En : Pt;
            var abs = value < 0 ? -value : value;
            foreach (var (div, pt, en) in Steps)
                if (abs >= div) return (value / div).ToString("0.#", culture) + (english ? en : pt);
            return value.ToString("0", culture);
        }
    }
}
