using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class SelfHealTests
    {
        [Fact]
        public void NoStacksOrInterval_NoHeal()
        {
            Assert.Equal(0f, SelfHeal.AmountPerTick(0, 0, 1000f, 1f, 0.25f));
            Assert.Equal(0f, SelfHeal.AmountPerTick(3, 3, 1000f, 1f, 0f));
        }

        [Fact]
        public void OneCopy_IsFlatOnly_IgnoresMaxHealth()
        {
            Assert.Equal(5f, SelfHeal.AmountPerTick(1, 0, 100f, 1f, 1f), 4);
            Assert.Equal(5f, SelfHeal.AmountPerTick(1, 0, 100000f, 1f, 1f), 4);
            Assert.Equal(15f, SelfHeal.AmountPerTick(0, 1, 100000f, 1f, 1f), 4);
        }

        [Fact]
        public void FlatGrowsWithLevel() => Assert.Equal(5f * 3f, SelfHeal.AmountPerTick(1, 0, 0f, 11f, 1f), 4);

        [Fact]
        public void ExtraCopies_AddPercentOfMaxHealth()
        {
            // 10 Egoístas, 2000 de vida: 5 + 0,01% * 9 * 2000 = 6,8/s
            Assert.Equal(6.8f, SelfHeal.AmountPerTick(10, 0, 2000f, 1f, 1f), 4);
            // 10 do Vazio: 15 + 0,5% * 9 * 2000 = 105/s
            Assert.Equal(105f, SelfHeal.AmountPerTick(0, 10, 2000f, 1f, 1f), 4);
        }
    }
}
