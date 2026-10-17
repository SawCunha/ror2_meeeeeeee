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
                _def.pickupIconSprite = LoadIcon(log) ?? MakeIcon();
                _def.pickupModelPrefab = ModItem.Model("RoR2/Base/EnergizedOnEquipmentUse/PickupWarHorn.prefab", "Horde", log);

                LanguageAPI.Add(Token + "_NAME", "Chamado da Horda");
                LanguageAPI.Add(Token + "_PICKUP", "Mais inimigos aparecem. Eles ficam mais difíceis de ignorar.");
                LanguageAPI.Add(Token + "_DESC",
                    "Aumenta o orçamento de spawn de inimigos em <style=cIsDamage>25%</style> " +
                    "<style=cStack>(+25% por acúmulo, até um limite)</style>.");
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

        // Arte embutida no plugin (Assets/chamado_da_horda.png, 256x256); null se falhar (cai no ícone gerado).
        private static Sprite LoadIcon(ManualLogSource log)
        {
            try
            {
                using (var stream = typeof(HordeItem).Assembly.GetManifestResourceStream("chamado_da_horda.png"))
                {
                    if (stream == null)
                    {
                        log.LogWarning("Horde: recurso do ícone não encontrado; usando ícone gerado.");
                        return null;
                    }
                    var bytes = new byte[stream.Length];
                    var read = 0;
                    while (read < bytes.Length)
                    {
                        var n = stream.Read(bytes, read, bytes.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(bytes, true))
                    {
                        log.LogWarning("Horde: falha ao decodificar o ícone; usando ícone gerado.");
                        return null;
                    }
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (Exception e)
            {
                log.LogWarning($"Horde: ícone indisponível ({e.GetType().Name}); usando ícone gerado.");
                return null;
            }
        }

        private static Sprite MakeIcon()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = new Color32(120, 30, 160, 255);
            var edge = new Color32(255, 215, 80, 255);
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    px[y * size + x] = (x < 6 || y < 6 || x >= size - 6 || y >= size - 6) ? edge : fill;
            tex.SetPixels32(px);
            tex.Apply(false, true); // sem cópia legível na RAM
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
