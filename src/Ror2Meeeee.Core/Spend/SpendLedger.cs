using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ror2Meeeee.Core.Spend
{
    public readonly struct SpendRow
    {
        public SpendRow(int id, string name, int purchases, long gold)
        {
            Id = id;
            Name = name;
            Purchases = purchases;
            Gold = gold;
        }

        public int Id { get; }
        public string Name { get; }
        public int Purchases { get; }
        public long Gold { get; }
    }

    // Acumula compras e ouro gasto por jogador. Puro: sem Unity nem jogo; o plugin resolve id e nome.
    public sealed class SpendLedger
    {
        private sealed class Entry
        {
            public string Name;
            public int Purchases;
            public long Gold;
        }

        private readonly Dictionary<int, Entry> _byId = new Dictionary<int, Entry>();

        public int Players => _byId.Count;

        public bool Knows(int id) => _byId.ContainsKey(id);

        // gold = custo pago em ouro (0 para compras em outro tipo de custo). `name` só é lido na primeira vez do id.
        public void Add(int id, Func<string> name, long gold)
        {
            if (!_byId.TryGetValue(id, out var e))
            {
                e = new Entry { Name = name() ?? ("#" + id.ToString(CultureInfo.InvariantCulture)) };
                _byId[id] = e;
            }
            e.Purchases++;
            if (gold > 0) e.Gold = long.MaxValue - e.Gold < gold ? long.MaxValue : e.Gold + gold;
        }

        public void Clear() => _byId.Clear();

        // Cópia ordenada por ouro gasto (maior primeiro), segura para entregar a outra thread.
        public SpendRow[] Snapshot()
        {
            var rows = new SpendRow[_byId.Count];
            var i = 0;
            foreach (var kv in _byId) rows[i++] = new SpendRow(kv.Key, kv.Value.Name, kv.Value.Purchases, kv.Value.Gold);
            Array.Sort(rows, (a, b) => b.Gold.CompareTo(a.Gold));
            return rows;
        }

        public static string Format(SpendRow[] rows)
        {
            if (rows.Length == 0) return "Nenhuma compra registrada nesta run.";
            var sb = new StringBuilder("Jogador | Compras | Ouro gasto");
            long total = 0;
            foreach (var r in rows)
            {
                sb.Append('\n').Append(r.Name).Append(" | ").Append(r.Purchases).Append(" | ").Append(r.Gold);
                total += r.Gold;
            }
            sb.Append("\nTotal | | ").Append(total);
            return sb.ToString();
        }
    }
}
