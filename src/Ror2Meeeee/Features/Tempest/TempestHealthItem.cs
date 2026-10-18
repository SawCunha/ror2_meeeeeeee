using System;
using System.Reflection;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;

#pragma warning disable CS0618 // pickupIconSprite: campo "left for modders' use" no 1.5.0

namespace Ror2Meeeee
{
    // Item oculto da Tormenta: cada stack dá +1% de vida máxima (BoostHp só anda de 10 em 10%).
    // O host dá aos monstros ao nascer (TempestFeature); o passo de vida é aplicado no BodyHooks.RecalculateStats,
    // em toda máquina, e replica pelo inventário.
    public static class TempestHealthItem
    {
        public const string Name = "Ror2MeeeeeTempestHp";
        public const string Token = "ROR2MEEEEE_ITEM_TEMPEST_HP";

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

        public static ItemDef Def => _def;

        // Chamar no Awake do plugin (antes do ItemCatalog).
        public static void Register(ManualLogSource log)
        {
            try
            {
                _def = ScriptableObject.CreateInstance<ItemDef>();
                _def.name = Name;
                _def.nameToken = Token + "_NAME";
                _def.pickupToken = Token + "_NAME";
                _def.descriptionToken = Token + "_NAME";
                _def.loreToken = Token + "_NAME";
                _def.canRemove = false;
                _def.hidden = true;
                _def.tags = new[] { ItemTag.AIBlacklist, ItemTag.CannotCopy, ItemTag.CannotSteal, ItemTag.BrotherBlacklist };
                typeof(ItemDef).GetField("deprecatedTier", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(_def, ItemTier.NoTier);
                _def.pickupIconSprite = TitheArtifact.MakeIcon(new Color32(120, 50, 170, 255));
                Loc.Add(Token + "_NAME", "Tempest Vigor", "Vigor da Tormenta");

                if (!ItemAPI.Add(new CustomItem(_def, Array.Empty<ItemDisplayRule>())))
                {
                    log.LogError("Tempest: ItemAPI.Add recusou o item de vida.");
                    _def = null;
                }
                else
                    log.LogInfo($"Tempest: item oculto '{Name}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Tempest: falha ao registrar o item de vida (o plugin segue sem ele): {e}");
                _def = null;
            }
        }
    }
}
