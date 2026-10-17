using System.Collections.Generic;
using Ror2Meeeee.Core.Eligibility;

namespace Ror2Meeeee.Core.Pipeline
{
    // Outro jogador que pode receber o item compartilhado.
    public readonly struct Recipient
    {
        // Guarda o stack atual do item no destinatário e se ele está vivo e conectado.
        public Recipient(int currentStack, bool alive, bool connected)
        {
            CurrentStack = currentStack;
            Alive = alive;
            Connected = connected;
        }

        public int CurrentStack { get; }
        public bool Alive { get; }
        public bool Connected { get; }
    }

    // Regra pura do compartilhamento (sem referência ao jogo): quantas cópias cada outro jogador recebe.
    public static class SharingPipeline
    {
        // Ordem: pickup -> multiplicação -> compartilhamento. Devolve, para cada destinatário (mesma ordem),
        // o TOTAL de cópias a conceder: Count * Multiplier (só Count se o tipo não multiplica), com StackCap
        // aplicado ao stack dele. 0 = nada (desligado, morto/desconectado, tipo não compartilhável). `fromSharing` = concessão que veio do próprio
        // compartilhamento: nunca é compartilhada de novo (FR-10).
        public static int[] Resolve(PickupContext ctx, MultiplierRules rules, bool enabled, bool fromSharing, IReadOnlyList<Recipient> others)
        {
            var result = new int[others.Count];
            if (!enabled || fromSharing || ctx.Count <= 0 || !EligibilityTable.IsShareable(ctx.Kind)) return result;

            for (var i = 0; i < others.Count; i++)
            {
                var o = others[i];
                if (!o.Alive || !o.Connected) continue;
                var extra = MultiplierPipeline.Resolve(
                    new PickupContext(ctx.Kind, ctx.Count, o.CurrentStack, o.Alive, o.Connected, ctx.CapExempt), rules);
                var total = (long)ctx.Count + extra;
                result[i] = (int)(total > int.MaxValue ? int.MaxValue : total);
            }
            return result;
        }
    }
}
