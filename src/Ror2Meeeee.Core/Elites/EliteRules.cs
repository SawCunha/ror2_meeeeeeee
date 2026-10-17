namespace Ror2Meeeee.Core.Elites
{
    // Regras dos elites do mod (Volátil, Frenético, Fortalecedor, Arruinado, Eco).
    public static class EliteRules
    {
        // Dois índices distintos de uma lista de `count`: `a` em [0, count) e `b` em [0, count - 1) vêm do sorteio.
        // Com count < 2 o segundo é -1 (só há um afixo disponível); com count < 1, os dois.
        public static (int First, int Second) PickTwoDistinct(int count, int a, int b)
        {
            if (count < 1) return (-1, -1);
            if (count < 2) return (0, -1);
            return (a, b >= a ? b + 1 : b);
        }
    }
}
