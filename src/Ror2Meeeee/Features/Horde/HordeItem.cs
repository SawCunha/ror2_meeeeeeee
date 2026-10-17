using System;
using System.Reflection;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;

#pragma warning disable CS0618 // pickupIconSprite/pickupModelPrefab: campos "left for modders' use" no 1.5.0

namespace Ror2Meeeee
{
    // Item do mod "Chamado da Horda": cada stack aumenta o orçamento de spawn de inimigos.
    // O efeito é aplicado pela DifficultyFeature (precisa de Difficulty.Enabled = true).
    // Branco (Tier1): fácil de obter e já elegível à multiplicação, sem IncludeLunar.
    public static class HordeItem
    {
        public const string Name = "Ror2MeeeeeHorde"; // XML-safe (exigência do R2API)
        public const string Token = "ROR2MEEEEE_ITEM_HORDE";

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

        // Chamar no Awake do plugin (antes de o ItemCatalog existir).
        public static void Register(ManualLogSource log)
        {
            try
            {
                _def = ScriptableObject.CreateInstance<ItemDef>();
                _def.name = Name;
                _def.nameToken = Token + "_NAME";
                _def.pickupToken = Token + "_PICKUP";
                _def.descriptionToken = Token + "_DESC";
                _def.loreToken = Token + "_LORE";
                _def.canRemove = true;
                _def.hidden = false;
                _def.tags = new[] { ItemTag.AIBlacklist };
                // ItemTierCatalog ainda não existe no Awake: a camada lê o campo serializado.
                typeof(ItemDef).GetField("deprecatedTier", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(_def, ItemTier.Tier1);
                _def.pickupIconSprite = TitheArtifact.LoadIcon("chamado_da_horda.png", new Color32(120, 30, 160, 255));
                _def.pickupModelPrefab = ModItem.Model("RoR2/Base/EnergizedOnEquipmentUse/PickupWarHorn.prefab", "Horde", log);

                LanguageAPI.Add(Token + "_NAME", "Chamado da Horda");
                LanguageAPI.Add(Token + "_PICKUP", "Mais inimigos aparecem. Eles ficam mais difíceis de ignorar.");
                LanguageAPI.Add(Token + "_DESC",
                    "Aumenta o orçamento de spawn de inimigos em <style=cIsDamage>50%</style> " +
                    "<style=cStack>(+50% por acúmulo, até um limite)</style>.");
                LanguageAPI.Add(Token + "_LORE", "Um chamado que ninguém pediu.");

                if (!ItemAPI.Add(new CustomItem(_def, Array.Empty<ItemDisplayRule>())))
                    log.LogError("Horde: ItemAPI.Add recusou o item.");
                else
                    log.LogInfo($"Horde: item '{Name}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Horde: falha ao registrar o item (o plugin segue sem ele): {e}");
                _def = null;
            }
        }
    }
}
