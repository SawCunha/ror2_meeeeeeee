using BepInEx.Logging;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    // Item vermelho "Cano Extra": cada cópia = +1 tiro em leque na arma principal (até CanoExtra.MaxExtraShots).
    // CannotCopy: torretas do Engineer não herdam. Item único para o Multiplicador (ItemClassifier).
    // O efeito é aplicado pela CanoFeature.
    public static class CanoItem
    {
        public const string Name = "Ror2MeeeeeCanoExtra";
        public const string Token = "ROR2MEEEEE_ITEM_CANOEXTRA";

        private static ItemDef _def;
        private static ItemIndex _index = ItemIndex.None;

        public static ItemIndex Index
        {
            get
            {
                if (_index == ItemIndex.None && _def) _index = _def.itemIndex;
                return _index;
            }
        }

        public static void Register(ManualLogSource log)
        {
            _def = ModItem.Register(Name, Token, ItemTier.Tier3, new[] { ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.CannotCopy },
                "cano_extra.png", new Color32(200, 50, 50, 255),
                "Cano Extra",
                "A cada stack adiciona 1 tiro múltiplo na arma principal.",
                "Sua <style=cIsDamage>arma principal</style> dispara <style=cIsDamage>+1 tiro</style> em leque " +
                "<style=cStack>(+1 por acúmulo, até um limite)</style>. Vale para balas, projéteis e flechas; " +
                "ataques corpo a corpo não são afetados. Não é multiplicado.",
                "Mais canos, mais problemas.",
                log, "RoR2/Base/Behemoth/PickupBehemoth.prefab");
        }
    }
}
