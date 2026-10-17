using System.Globalization;

namespace Ror2Meeeee.Core.Stats
{
    // Números grandes legíveis em pt-BR para a tela de status: 950, 12,3 mil, 4,5 mi, 1,2 bi, 3,4 tri.
    public static class ShortNumber
    {
        private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly (double Div, string Suffix)[] Steps =
        {
            (1e12, " tri"), (1e9, " bi"), (1e6, " mi"), (1e3, " mil"),
        };

        public static string Format(double value)
        {
            var abs = value < 0 ? -value : value;
            foreach (var (div, suffix) in Steps)
                if (abs >= div) return (value / div).ToString("0.#", Pt) + suffix;
            return value.ToString("0", Pt);
        }
    }
}
