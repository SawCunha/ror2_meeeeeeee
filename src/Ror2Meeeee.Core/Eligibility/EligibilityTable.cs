using Ror2Meeeee.Core.Pipeline;

namespace Ror2Meeeee.Core.Eligibility
{
    // Tipo de item já classificado pelo plugin a partir do jogo (tier, tags, tipo de pickup).
    // Special = tiers das expansões sem regra de multiplicação (FoodTier, Cursed, Page, Hex, NoTier...).
    public enum ItemKind { White, Green, Red, Boss, Lunar, Void, Temporary, Scrap, Equipment, Other, Special }

    // Diz quais tipos de item o Multiplicador multiplica e quais o Comunismo compartilha.
    public static class EligibilityTable
    {
        // Multiplicador: comuns, incomuns, raros e chefe sempre; lunar e void conforme a config; o resto nunca.
        public static bool IsEligible(ItemKind kind, MultiplierRules rules)
        {
            switch (kind)
            {
                case ItemKind.White:
                case ItemKind.Green:
                case ItemKind.Red:
                case ItemKind.Boss:
                    return true;
                case ItemKind.Lunar:
                    return rules.IncludeLunar;
                case ItemKind.Void:
                    return rules.IncludeVoid;
                default: // Temporary, Scrap, Equipment, Other, Special nunca multiplicam
                    return false;
            }
        }

        // Artefato do Comunismo: todo item é compartilhado, exceto equipamento e os itens únicos do mod (Other).
        public static bool IsShareable(ItemKind kind) => kind != ItemKind.Equipment && kind != ItemKind.Other;
    }
}
