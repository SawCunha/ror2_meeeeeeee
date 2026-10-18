using RoR2;
using Ror2Meeeee.Core.Eligibility;

namespace Ror2Meeeee
{
    // Classificação do jogo -> tabela pura do Core. Tiers sem regra (expansões, NoTier) viram Special: não multiplicam,
    // mas o Comunismo compartilha.
    internal static class ItemClassifier
    {
        // Itens do mod que nunca multiplicam nem são compartilhados (1 cópia por pickup): Capitalismo, Cano Extra e
        // Chamado da Horda (multiplicado e compartilhado entre 3-4 jogadores, chegava a x43 inimigos e travava o jogo)
        // e o item oculto de vida da Tormenta (sem tier: viraria Special, que o Comunismo compartilha).
        public static bool IsUnique(ItemIndex index) =>
            index != ItemIndex.None && (TaxReliefItems.IsReliefItem(index) || index == CanoItem.Index || index == HordeItem.Index || index == TempestHealthItem.Index);

        // Converte o ItemDef do jogo (tier, tag de sucata, tipo de pickup) no ItemKind usado pelas regras do Core.
        public static ItemKind Classify(ItemDef def, bool temporaryPickup)
        {
            if (temporaryPickup) return ItemKind.Temporary;
            if (IsUnique(def.itemIndex)) return ItemKind.Other; // nunca multiplicado nem compartilhado
            if (def.ContainsTag(ItemTag.Scrap)) return ItemKind.Scrap;
            switch (def.tier)
            {
                case ItemTier.Tier1: return ItemKind.White;
                case ItemTier.Tier2: return ItemKind.Green;
                case ItemTier.Tier3: return ItemKind.Red;
                case ItemTier.Boss: return ItemKind.Boss;
                case ItemTier.Lunar: return ItemKind.Lunar;
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss: return ItemKind.Void;
                default: return ItemKind.Special;
            }
        }
    }
}
