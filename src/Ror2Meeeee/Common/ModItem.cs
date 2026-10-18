using System;
using System.Reflection;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

#pragma warning disable CS0618 // pickupIconSprite/pickupModelPrefab: campos "left for modders' use" no 1.5.0

namespace Ror2Meeeee
{
    // Registro de item simples (sem display rules), para os itens do mod. Chamar no Awake do plugin
    // (antes do ItemCatalog). Devolve null se falhar; o plugin segue sem o item.
    internal static class ModItem
    {
        public static ItemDef Register(string name, string token, ItemTier tier, ItemTag[] tags,
            string iconResource, Color32 iconFallback,
            (string Name, string Pickup, string Desc, string Lore) en, (string Name, string Pickup, string Desc, string Lore) pt,
            ManualLogSource log, string modelKey = null)
        {
            try
            {
                var def = ScriptableObject.CreateInstance<ItemDef>();
                def.name = name; // XML-safe (exigência do R2API)
                def.nameToken = token + "_NAME";
                def.pickupToken = token + "_PICKUP";
                def.descriptionToken = token + "_DESC";
                def.loreToken = token + "_LORE";
                def.canRemove = true;
                def.hidden = false;
                def.tags = tags;
                // ItemTierCatalog ainda não existe no Awake: a camada lê o campo serializado.
                typeof(ItemDef).GetField("deprecatedTier", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(def, tier);
                def.pickupIconSprite = TitheArtifact.LoadIcon(iconResource, iconFallback);
                def.pickupModelPrefab = Model(modelKey, name, log);

                Loc.Add(token + "_NAME", en.Name, pt.Name);
                Loc.Add(token + "_PICKUP", en.Pickup, pt.Pickup);
                Loc.Add(token + "_DESC", en.Desc, pt.Desc);
                Loc.Add(token + "_LORE", en.Lore, pt.Lore);

                if (!ItemAPI.Add(new CustomItem(def, Array.Empty<ItemDisplayRule>())))
                {
                    log.LogError($"{name}: ItemAPI.Add recusou o item.");
                    return null;
                }
                log.LogInfo($"{name}: item registrado.");
                return def;
            }
            catch (Exception e)
            {
                log.LogError($"{name}: falha ao registrar o item (o plugin segue sem ele): {e}");
                return null;
            }
        }

        // Par "contagioso" do DLC1: o item do Vazio converte as cópias do normal (como Weeping Fungus/Bustling Fungus).
        public static void Corrupt(ItemDef normal, ItemDef voidItem, ManualLogSource log)
        {
            if (!normal || !voidItem) return;
            try
            {
                var type = Addressables.LoadAssetAsync<ItemRelationshipType>("RoR2/DLC1/Common/ContagiousItem.asset").WaitForCompletion();
                if (!type) { log.LogWarning($"{voidItem.name}: tipo ContagiousItem indisponível; não corrompe."); return; }
                var provider = ScriptableObject.CreateInstance<ItemRelationshipProvider>();
                provider.name = voidItem.name + "Contagion";
                provider.relationshipType = type;
                provider.relationships = new[] { new ItemDef.Pair { itemDef1 = normal, itemDef2 = voidItem } };
                if (!ContentAddition.AddItemRelationshipProvider(provider)) log.LogWarning($"{voidItem.name}: ContentAddition recusou a corrupção.");
            }
            catch (Exception e)
            {
                log.LogWarning($"{voidItem.name}: falha ao registrar a corrupção (itens seguem sem ela): {e.Message}");
            }
        }

        // Modelo de pickup de um item do jogo (chave Addressables, ex.: "RoR2/Base/Hoof/PickupHoof.prefab").
        // Sem chave ou se não carregar: a "caixa misteriosa" do jogo; null só se nem ela carregar (item invisível no chão).
        public static GameObject Model(string key, string owner, ManualLogSource log)
        {
            if (key != null)
            {
                try
                {
                    var model = Addressables.LoadAssetAsync<GameObject>(key).WaitForCompletion();
                    if (model) return model;
                    log.LogWarning($"{owner}: modelo {key} indisponível; usando o placeholder.");
                }
                catch (Exception e) { log.LogWarning($"{owner}: modelo {key} indisponível ({e.GetType().Name}); usando o placeholder."); }
            }
            try { return LegacyResourcesAPI.Load<GameObject>("Prefabs/PickupModels/PickupMystery"); }
            catch (Exception e)
            {
                log.LogWarning($"{owner}: modelo placeholder indisponível ({e.GetType().Name}); o item ficará invisível no chão.");
                return null;
            }
        }
    }
}
