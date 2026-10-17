using System;
using Ror2Meeeee.Core.Eligibility;

namespace Ror2Meeeee.Core.Pipeline
{
    public readonly struct PickupContext
    {
        public PickupContext(ItemKind kind, int count, int currentStack, bool playerAlive, bool playerConnected, bool capExempt)
        {
            Kind = kind;
            Count = count;
            CurrentStack = currentStack;
            PlayerAlive = playerAlive;
            PlayerConnected = playerConnected;
            CapExempt = capExempt;
        }

        public ItemKind Kind { get; }
        public int Count { get; }          // cópias do pickup original (já concedidas)
        public int CurrentStack { get; }   // stack do jogador ANTES do pickup
        public bool PlayerAlive { get; }
        public bool PlayerConnected { get; }
        public bool CapExempt { get; }     // item sob LimitedCalc: o teto não vale
    }

    public readonly struct MultiplierRules
    {
        public const int DefaultMultiplier = 10; // padrão do perfil de jogo (era 100, para validar desempenho)

        public MultiplierRules(int multiplier, bool includeLunar, bool includeVoid, int stackCap)
        {
            Multiplier = multiplier <= 0 ? DefaultMultiplier : multiplier;
            IncludeLunar = includeLunar;
            IncludeVoid = includeVoid;
            StackCap = stackCap < 0 ? 0 : stackCap;
        }

        public int Multiplier { get; }
        public bool IncludeLunar { get; }
        public bool IncludeVoid { get; }
        public int StackCap { get; }       // 0 = sem teto
    }

    public static class MultiplierPipeline
    {
        // Teto rígido: com o stack já no teto, nem a cópia original entra (o pickup some; o compartilhamento não dá nada).
        public static bool AtHardCap(int currentStack, MultiplierRules rules) => rules.StackCap > 0 && currentStack >= rules.StackCap;

        // Cópias EXTRAS a conceder (além das Count que o jogo já concedeu). 0 = nada.
        public static int Resolve(PickupContext ctx, MultiplierRules rules)
        {
            if (!ctx.PlayerAlive || !ctx.PlayerConnected) return 0;
            if (ctx.Count <= 0 || !EligibilityTable.IsEligible(ctx.Kind, rules)) return 0;

            var total = (long)ctx.Count * rules.Multiplier;
            if (total > int.MaxValue) total = int.MaxValue;
            var extra = total - ctx.Count;

            if (rules.StackCap > 0 && !ctx.CapExempt)
            {
                var room = rules.StackCap - ((long)ctx.CurrentStack + ctx.Count);
                extra = Math.Min(extra, Math.Max(0, room));
            }

            // Nunca deixar o stack final passar de int.MaxValue.
            var maxExtra = int.MaxValue - ((long)ctx.CurrentStack + ctx.Count);
            return (int)Math.Max(0, Math.Min(extra, maxExtra));
        }
    }
}
