using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    // Itens soltos pelo Drop e ainda não pegos de volta. Quem pega um item solto NÃO deve multiplicá-lo nem
    // compartilhá-lo de novo (senão soltar + pegar vira duplicação: 4 soltos viravam 4 x multiplicador).
    // Multiplier e Sharing consultam a mesma concessão: o resultado é memorizado por frame+item (idempotente).
    // ponytail: duas concessões do mesmo item no mesmo frame contam como uma; custo: no máximo 1 multiplicação perdida/dobrada.
    internal static class DroppedGrants
    {
        private static readonly Dictionary<ItemIndex, int> Pending = new Dictionary<ItemIndex, int>();
        private static int _frame = -1;
        private static ItemIndex _index = ItemIndex.None;
        private static bool _result;

        public static void Note(ItemIndex index, int count)
        {
            if (count <= 0) return;
            Pending.TryGetValue(index, out var n);
            Pending[index] = n + count;
        }

        public static bool IsDropped(ItemIndex index)
        {
            var frame = Time.frameCount;
            if (frame == _frame && index == _index) return _result;
            _frame = frame;
            _index = index;
            _result = false;
            if (Pending.TryGetValue(index, out var n) && n > 0)
            {
                if (n == 1) Pending.Remove(index); else Pending[index] = n - 1;
                _result = true;
            }
            return _result;
        }

        public static void Clear()
        {
            Pending.Clear();
            _frame = -1;
        }
    }
}
