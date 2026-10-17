using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;

namespace Ror2Meeeee
{
    // Item void verde "zzz cervo <3": toda a velocidade de movimento extra vira dano.
    // Não corrompe nenhum item; sai em Void Cradles/Potentials. O efeito é aplicado pela CervoFeature.
    public static class CervoItem
    {
        public const string Name = "Ror2MeeeeeCervo";
        public const string Token = "ROR2MEEEEE_ITEM_CERVO";

        private static ItemDef _def;
        private static ItemIndex _index = ItemIndex.None;

        // ItemIndex só existe depois do ItemCatalog; resolvido uma vez e guardado.
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
            var extra = (int)(SpeedToDamage.ExtraStackRatio * 100f);
            _def = ModItem.Register(Name, Token, ItemTier.VoidTier2, new[] { ItemTag.Damage, ItemTag.AIBlacklist },
                "cervo.png", new Color32(150, 60, 170, 255),
                "zzz cervo <3",
                "A pressa é inimiga da perfeição.",
                "Converte toda a <style=cIsUtility>velocidade de movimento adicional</style> em <style=cIsDamage>dano</style>: " +
                "cada +1% de velocidade vira <style=cIsDamage>+1% de dano</style> " +
                $"<style=cStack>(+{extra}% de conversão por acúmulo)</style>. Você fica na velocidade normal; lentidões continuam valendo.",
                "A pressa é inimiga da perfeição.",
                log, "RoR2/Base/Hoof/PickupHoof.prefab");
        }
    }
}
