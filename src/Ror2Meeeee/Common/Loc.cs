using System.Globalization;
using R2API;
using RoR2;

namespace Ror2Meeeee
{
    // Idiomas: inglês é a base (vale para qualquer idioma do jogo); pt-BR sobrescreve.
    // "Taxade" é termo do mod e fica igual nos dois.
    internal static class Loc
    {
        public const string PtBr = "pt-BR";
        public static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo(PtBr);
        public static readonly CultureInfo EnCulture = CultureInfo.InvariantCulture;

        public static bool IsPt => Language.currentLanguageName == PtBr;

        // Texto montado na hora, no idioma atual do jogo (telas locais, respostas de comando).
        public static string T(string en, string pt) => IsPt ? pt : en;

        // Token do jogo (itens, artefatos, elites, chat): cada cliente resolve no próprio idioma.
        public static void Add(string token, string en, string pt)
        {
            LanguageAPI.Add(token, en);
            LanguageAPI.Add(token, pt, PtBr);
        }

        // Chat do host para todos: manda o token e os parâmetros; cada cliente monta a frase no seu idioma
        // (SimpleChatMessage faz string.Format com paramTokens). Os parâmetros vão como chegam (nomes já resolvidos no host).
        public static void Broadcast(string token, params string[] args)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = token, paramTokens = args });
        }
    }
}
