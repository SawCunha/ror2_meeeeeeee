using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using R2API;
using RoR2;
using Ror2Meeeee.Core.Tax;
using UnityEngine;

#pragma warning disable CS0618 // pickupIconSprite/pickupModelPrefab: campos "left for modders' use" no 1.5.0

namespace Ror2Meeeee
{
    // Três itens que reduzem a taxa do Artefato Taxade (branco 20%, verde 30%, vermelho 50%) e aumentam o ouro
    // ganho (+10%, +20%, +30%). Regras: no máximo 1 de cada por jogador, nunca duplicados
    // (printer/duplicator/multiplicador/compartilhamento) e trocados no baú de quem já tem.
    // Os efeitos são aplicados pela TaxFeature; os percentuais vêm de Core.Tax.TaxRelief.
    public static class TaxReliefItems
    {
        private sealed class Def
        {
            public string Name, Token, PtName;
            public ItemTier Tier;
            public int Percent;
            public int GoldPercent;
            public Color32 Fallback;
            public ItemDef Item;
            public ItemIndex Index = ItemIndex.None;
        }

        private static readonly Def White = new Def
        {
            Name = "Ror2MeeeeeTaxReliefWhite", Token = "ROR2MEEEEE_ITEM_TAXRELIEF_WHITE", PtName = "Capitalismo",
            Tier = ItemTier.Tier1, Percent = TaxRelief.WhitePercent, GoldPercent = TaxRelief.WhiteGoldPercent, Fallback = new Color32(235, 235, 235, 255),
        };
        private static readonly Def Green = new Def
        {
            Name = "Ror2MeeeeeTaxReliefGreen", Token = "ROR2MEEEEE_ITEM_TAXRELIEF_GREEN", PtName = "Capitalismo",
            Tier = ItemTier.Tier2, Percent = TaxRelief.GreenPercent, GoldPercent = TaxRelief.GreenGoldPercent, Fallback = new Color32(80, 190, 90, 255),
        };
        private static readonly Def Red = new Def
        {
            Name = "Ror2MeeeeeTaxReliefRed", Token = "ROR2MEEEEE_ITEM_TAXRELIEF_RED", PtName = "Capitalismo",
            Tier = ItemTier.Tier3, Percent = TaxRelief.RedPercent, GoldPercent = TaxRelief.RedGoldPercent, Fallback = new Color32(200, 50, 50, 255),
        };

        private static readonly Def[] All = { White, Green, Red };

        // Chamar no Awake do plugin (antes de o ItemCatalog existir). Falha não derruba o plugin.
        public static void Register(ManualLogSource log)
        {
            foreach (var d in All) RegisterOne(d, log);
            try { Inventory.onServerItemGiven += OnItemGiven; }
            catch (Exception e) { log.LogError($"TaxRelief: falha ao ligar o limite de 1 por jogador: {e}"); }
        }

        private static void RegisterOne(Def d, ManualLogSource log)
        {
            try
            {
                var item = ScriptableObject.CreateInstance<ItemDef>();
                item.name = d.Name;
                item.nameToken = d.Token + "_NAME";
                item.pickupToken = d.Token + "_PICKUP";
                item.descriptionToken = d.Token + "_DESC";
                item.loreToken = d.Token + "_LORE";
                item.canRemove = true;
                item.hidden = false;
                // CannotDuplicate/CannotCopy: fora de printer, duplicator, mimic e cópias do jogo.
                item.tags = new[] { ItemTag.AIBlacklist, ItemTag.CannotDuplicate, ItemTag.CannotCopy };
                typeof(ItemDef).GetField("deprecatedTier", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(item, d.Tier);
                // Mesma arte para os três tiers (Assets/artifacts/capitalismo.png); sem ela cai no ícone gerado.
                item.pickupIconSprite = TitheArtifact.LoadIcon("capitalismo.png", d.Fallback);
                item.pickupModelPrefab = ModItem.Model("RoR2/DLC1/GoldOnHurt/PickupRollOfPennies.prefab", d.Name, log);

                Loc.Add(d.Token + "_NAME", "Capitalism", d.PtName);
                Loc.Add(d.Token + "_PICKUP", $"Reduces the Artifact of Taxade's tax by {d.Percent}% and gives +{d.GoldPercent}% gold.", $"Reduz em {d.Percent}% a taxa do Artefato Taxade e dá +{d.GoldPercent}% de ouro.");
                Loc.Add(d.Token + "_DESC",
                    $"Reduces the health lost on gold purchases under the Artifact of Taxade by <style=cIsUtility>{d.Percent}%</style> " +
                    $"and increases all gold earned by <style=cShrine>{d.GoldPercent}%</style>. " +
                    "Only 1 per player; doesn't stack or duplicate, and no longer drops from chests for those who already have it.",
                    $"Reduz a vida perdida nas compras com ouro sob o Artefato Taxade em <style=cIsUtility>{d.Percent}%</style> " +
                    $"e aumenta todo ouro ganho em <style=cShrine>{d.GoldPercent}%</style>. " +
                    "Só 1 por jogador; não acumula nem duplica, e não sai mais em baús de quem já tem.");
                Loc.Add(d.Token + "_LORE", "Someone always finds a loophole.", "Alguém sempre encontra uma brecha.");

                if (!ItemAPI.Add(new CustomItem(item, Array.Empty<ItemDisplayRule>())))
                    log.LogError($"TaxRelief: ItemAPI.Add recusou '{d.Name}'.");
                else
                {
                    d.Item = item;
                    log.LogInfo($"TaxRelief: item '{d.Name}' ({d.Percent}%) registrado.");
                }
            }
            catch (Exception e)
            {
                log.LogError($"TaxRelief: falha ao registrar '{d.Name}' (o plugin segue sem ele): {e}");
                d.Item = null;
            }
        }

        // ItemIndex só existe depois do ItemCatalog; resolvido uma vez.
        private static ItemIndex IndexOf(Def d)
        {
            if (d.Index == ItemIndex.None && d.Item) d.Index = d.Item.itemIndex;
            return d.Index;
        }

        public static bool IsReliefItem(ItemIndex index)
        {
            if (index == ItemIndex.None) return false;
            foreach (var d in All)
                if (IndexOf(d) == index) return true;
            return false;
        }

        private static bool Has(Inventory inv, Def d)
        {
            var idx = IndexOf(d);
            return idx != ItemIndex.None && inv.GetItemCountPermanent(idx) > 0;
        }

        // Fator para o dano da taxa deste inventário (1 = sem alívio).
        public static float TaxMultiplier(Inventory inv)
        {
            return inv ? TaxRelief.Multiplier(Has(inv, White), Has(inv, Green), Has(inv, Red)) : 1f;
        }

        private static bool _loggedFailure;

        // Ouro com o bônus dos itens deste inventário (sem itens = mesmo valor).
        public static uint BoostGold(Inventory inv, uint amount)
        {
            return inv ? TaxRelief.Gold(amount, Has(inv, White), Has(inv, Green), Has(inv, Red)) : amount;
        }

        // Para a troca no baú: reaproveitada, sem alocar por compra.
        private static readonly List<PickupIndex> Candidates = new List<PickupIndex>();
        private static readonly PropertyInfo CurrentPickupProperty =
            typeof(ChestBehavior).GetProperty("currentPickup", BindingFlags.Instance | BindingFlags.Public);

        // Se o baú vai soltar um Capitalismo que quem abriu já tem, troca por outro item do mesmo tier
        // (sem Capitalismo). Só no host, antes do ItemDrop (que roda depois da animação de abertura).
        // `rng` é do mod: não consome o RNG da run nem o do baú.
        public static bool TryReplaceOwnedDrop(ChestBehavior chest, Inventory opener, Xoroshiro128Plus rng, out ItemIndex replaced, out PickupIndex replacement)
        {
            replaced = ItemIndex.None;
            replacement = PickupIndex.none;
            var pickup = chest.currentPickup;
            if (!pickup.isValid) return false;
            var def = PickupCatalog.GetPickupDef(pickup.pickupIndex);
            var item = def != null ? def.itemIndex : ItemIndex.None;
            Def owned = null;
            foreach (var d in All)
                if (IndexOf(d) == item) { owned = d; break; }
            if (owned == null || opener.GetItemCountPermanent(item) <= 0) return false;

            var run = Run.instance;
            if (!run || CurrentPickupProperty == null) return false;
            var pool = owned.Tier == ItemTier.Tier1 ? run.availableTier1DropList
                : owned.Tier == ItemTier.Tier2 ? run.availableTier2DropList
                : run.availableTier3DropList;
            Candidates.Clear();
            for (var i = 0; i < pool.Count; i++)
            {
                var c = PickupCatalog.GetPickupDef(pool[i]);
                if (c != null && !IsReliefItem(c.itemIndex)) Candidates.Add(pool[i]);
            }
            if (Candidates.Count == 0) return false;

            pickup.pickupIndex = rng.NextElementUniform(Candidates); // mantém rental/temporário do baú
            CurrentPickupProperty.SetValue(chest, pickup); // setter privado; 1 boxing por troca (raro)
            Candidates.Clear();
            replaced = item;
            replacement = pickup.pickupIndex;
            return true;
        }

        // Todas as origens de concessão (pickup, printer, comando) passam por aqui: excedente é removido.
        // Evento quente: dispara para TODO item dado no host (inclusive Boost* em cada inimigo e as cópias do
        // Multiplier), então o filtro barato e seletivo (comparar índices) vem antes de qualquer checagem do Unity.
        // RemoveItemPermanent dá contagem negativa, que não redispara este evento.
        private static void OnItemGiven(Inventory inv, ItemIndex item, int count)
        {
            try
            {
                if (!IsReliefItem(item) || !UnityEngine.Networking.NetworkServer.active || !inv) return;
                var have = inv.GetItemCountPermanent(item);
                if (have > 1) inv.RemoveItemPermanent(item, have - 1);
            }
            catch (Exception e)
            {
                // Sem Feature/Fail aqui: loga só a primeira para não inundar o log num evento tão frequente.
                if (_loggedFailure) return;
                _loggedFailure = true;
                Debug.LogError($"[Ror2Meeeee] TaxRelief: falha ao limitar a 1 por jogador (próximas omitidas): {e}");
            }
        }
    }
}
