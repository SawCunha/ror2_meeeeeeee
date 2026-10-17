using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Helluva;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Comando ror2meeeee_drop: o jogador solta itens do PRÓPRIO inventário no chão (para dar a um aliado, que pega
    // normalmente). Sem hooks: só roda quando o comando é usado. Só no host (o comando executa no servidor).
    public sealed class DropFeature : Feature
    {
        private readonly ConfigEntry<int> _max;

        public DropFeature(Cfg cfg, ManualLogSource log) : base("Drop", cfg, log,
            "Comando de console ror2meeeee_drop: solta itens do próprio inventário no chão (para dar a um aliado). Sem hooks.",
            enabledByDefault: true)
        {
            _max = cfg.Bind("Drop", "MaxPerCommand", 10, "Máximo de unidades soltas por comando (evita inundar o chão e travar o jogo).");
        }

        // Teto de unidades por comando (a tela F7 usa o mesmo valor para não prometer mais do que o servidor solta).
        internal int MaxPerCommand => _max.Value;

        public override void ResetRunState() => DroppedGrants.Clear();

        protected override void OnEnable() { }
        protected override void OnDisable() { }

        // Itens que podem sair do inventário: visíveis, com tier e removíveis.
        internal static bool Droppable(ItemDef d) => d && !d.hidden && d.tier != ItemTier.NoTier && d.canRemove;

        private static string DisplayName(ItemDef d) => Language.GetString(d.nameToken);

        // Devolve o texto para o console. query vazio = lista o que dá para soltar.
        public string Execute(CharacterMaster master, string query, int requested)
        {
            try
            {
                if (!NetworkServer.active) return "Só funciona no host.";
                var body = master ? master.GetBody() : null;
                if (!body || !body.healthComponent || !body.healthComponent.alive) return "Você precisa estar vivo.";
                var inv = master.inventory;
                if (!inv) return "Sem inventário.";

                var owned = new List<ItemDef>();
                foreach (var idx in inv.itemAcquisitionOrder)
                {
                    var d = ItemCatalog.GetItemDef(idx);
                    if (Droppable(d) && inv.GetItemCountPermanent(idx) > 0) owned.Add(d);
                }
                if (owned.Count == 0) return "Você não tem itens que possam ser soltos.";

                if (string.IsNullOrWhiteSpace(query)) return "Itens que você pode soltar:\n" + Describe(owned, inv) + "\nUso: ror2meeeee_drop <nome> [quantidade]";

                var matches = Match(owned, query.Trim());
                if (matches.Count == 0) return $"Nenhum item seu combina com '{query}'.\n" + Describe(owned, inv);
                if (matches.Count > 1) return $"'{query}' é ambíguo; seja mais específico:\n" + Describe(matches, inv);

                var def = matches[0];
                var have = inv.GetItemCountPermanent(def.itemIndex);
                var count = DropRules.ClampCount(requested, have, _max.Value);
                if (count <= 0) return "Nada para soltar (Drop.MaxPerCommand = 0?).";

                inv.RemoveItemPermanent(def.itemIndex, count);
                DroppedGrants.Note(def.itemIndex, count); // pegar de volta não multiplica nem compartilha
                var pickup = PickupCatalog.FindPickupIndex(def.itemIndex);
                var origin = body.corePosition + Vector3.up * 1.5f;
                for (var i = 0; i < count; i++)
                {
                    // Leque em volta do jogador; cada unidade vira um pickup separado.
                    var dir = Quaternion.AngleAxis(360f * i / count, Vector3.up) * Vector3.forward;
                    PickupDropletController.CreatePickupDroplet(new UniquePickup(pickup), origin, dir * 6f + Vector3.up * 15f, isDuplicated: false);
                }

                var who = Util.GetBestMasterName(master);
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#98c379>Drop:</color> {who} largou {count}x {DisplayName(def)} no chão."
                });
                Log.LogInfo($"[Drop] {who} soltou {count}x {def.name} (tinha {have})");
                return $"Solto: {count}x {DisplayName(def)}.";
            }
            catch (Exception e)
            {
                Fail(e);
                return "Falha ao soltar o item (veja o log).";
            }
        }

        // Nome interno exato > nome exibido exato > contém em qualquer um dos dois.
        private static List<ItemDef> Match(List<ItemDef> items, string q)
        {
            var exact = items.FindAll(d => string.Equals(d.name, q, StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(DisplayName(d), q, StringComparison.OrdinalIgnoreCase));
            if (exact.Count > 0) return exact;
            return items.FindAll(d => d.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                      || DisplayName(d).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string Describe(List<ItemDef> items, Inventory inv)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < items.Count; i++)
                sb.Append("  ").Append(DisplayName(items[i])).Append(" (").Append(items[i].name).Append(") x")
                  .Append(inv.GetItemCountPermanent(items[i].itemIndex)).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }
    }
}
