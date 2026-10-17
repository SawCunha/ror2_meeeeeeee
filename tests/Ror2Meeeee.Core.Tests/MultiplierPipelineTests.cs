using Ror2Meeeee.Core.Eligibility;
using Ror2Meeeee.Core.Pipeline;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class MultiplierPipelineTests
    {
        private static PickupContext Pickup(ItemKind kind = ItemKind.White, int count = 1, int stack = 0,
            bool alive = true, bool connected = true, bool capExempt = false)
            => new PickupContext(kind, count, stack, alive, connected, capExempt);

        private static MultiplierRules Rules(int mult = 2, bool lunar = false, bool voidItems = false, int cap = 0)
            => new MultiplierRules(mult, lunar, voidItems, cap);

        [Fact]
        public void Multiplier1_GrantsNoExtra() => Assert.Equal(0, MultiplierPipeline.Resolve(Pickup(), Rules(1)));

        [Fact]
        public void Multiplier2_GrantsOneExtraCopy() => Assert.Equal(1, MultiplierPipeline.Resolve(Pickup(), Rules(2)));

        [Fact]
        public void Multiplier100_Grants99Extra() => Assert.Equal(99, MultiplierPipeline.Resolve(Pickup(), Rules(100)));

        [Fact]
        public void CountGreaterThanOne_MultipliesTheWholeGrant() =>
            Assert.Equal(6, MultiplierPipeline.Resolve(Pickup(count: 3), Rules(3)));

        [Fact]
        public void HugeMultiplier_SaturatesWithoutOverflow()
        {
            var extra = MultiplierPipeline.Resolve(Pickup(count: 5), Rules(int.MaxValue));
            Assert.Equal(int.MaxValue - 5, extra);
        }

        [Theory]
        [InlineData(0, MultiplierRules.DefaultMultiplier)]
        [InlineData(-4, MultiplierRules.DefaultMultiplier)]
        public void InvalidMultiplier_FallsBackToDefault(int bad, int expectedMultiplier)
        {
            var rules = Rules(bad);
            Assert.Equal(expectedMultiplier, rules.Multiplier);
            Assert.Equal(expectedMultiplier - 1, MultiplierPipeline.Resolve(Pickup(), rules));
        }

        [Theory]
        [InlineData(ItemKind.White, false, false, true)]
        [InlineData(ItemKind.Green, false, false, true)]
        [InlineData(ItemKind.Red, false, false, true)]
        [InlineData(ItemKind.Boss, false, false, true)]
        [InlineData(ItemKind.Lunar, false, false, false)]
        [InlineData(ItemKind.Lunar, true, false, true)]
        [InlineData(ItemKind.Void, false, false, false)]
        [InlineData(ItemKind.Void, false, true, true)]
        [InlineData(ItemKind.Temporary, true, true, false)]
        [InlineData(ItemKind.Scrap, true, true, false)]
        [InlineData(ItemKind.Equipment, true, true, false)]
        [InlineData(ItemKind.Other, true, true, false)]
        public void Eligibility_ByKind(ItemKind kind, bool lunar, bool voidItems, bool eligible)
        {
            Assert.Equal(eligible, EligibilityTable.IsEligible(kind, Rules(2, lunar, voidItems)));
            Assert.Equal(eligible ? 1 : 0, MultiplierPipeline.Resolve(Pickup(kind), Rules(2, lunar, voidItems)));
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void DeadOrDisconnectedPlayer_GetsNothing(bool alive, bool connected)
        {
            Assert.Equal(0, MultiplierPipeline.Resolve(Pickup(alive: alive, connected: connected), Rules(2)));
        }

        // Story 3.4: teto de stacks, checado DEPOIS da multiplicação.
        [Theory]
        [InlineData(0, 99)]   // sem teto
        [InlineData(10, 9)]   // stack 0 + 1 base + extra -> teto 10 => extra 9
        [InlineData(1, 0)]    // teto igual ao concedido
        public void StackCap_ReducesExtra(int cap, int expectedExtra)
        {
            Assert.Equal(expectedExtra, MultiplierPipeline.Resolve(Pickup(), Rules(100, cap: cap)));
        }

        [Fact]
        public void StackCap_AlreadyAbove_GrantsNoExtra() =>
            Assert.Equal(0, MultiplierPipeline.Resolve(Pickup(stack: 50), Rules(100, cap: 10)));

        [Fact]
        public void StackCap_ExemptItem_IsIgnored() =>
            Assert.Equal(99, MultiplierPipeline.Resolve(Pickup(stack: 50, capExempt: true), Rules(100, cap: 10)));

        [Fact]
        public void StackCap_NearIntMax_DoesNotOverflow() =>
            Assert.Equal(0, MultiplierPipeline.Resolve(Pickup(stack: int.MaxValue - 1), Rules(100, cap: int.MaxValue)));
    }
}
