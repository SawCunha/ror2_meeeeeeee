using Ror2Meeeee.Core.RustyNail;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class RustyNailRulesTests
    {
        [Fact]
        public void Chance_TwoPercentPlusPerExtraCopy_CappedAtHundred()
        {
            Assert.Equal(0f, RustyNailRules.ChancePercent(0, RustyNailRules.ChancePerStack));
            Assert.Equal(2f, RustyNailRules.ChancePercent(1, RustyNailRules.ChancePerStack), 4);
            Assert.Equal(2.2f, RustyNailRules.ChancePercent(3, RustyNailRules.ChancePerStack), 4);
            Assert.Equal(3f, RustyNailRules.ChancePercent(3, RustyNailRules.VoidChancePerStack), 4);
            Assert.Equal(100f, RustyNailRules.ChancePercent(1000, RustyNailRules.ChancePerStack), 4);
        }

        [Fact]
        public void Nail_TenPercentPlusPerExtraCopy()
        {
            Assert.Equal(0f, RustyNailRules.NailCoefficient(0, RustyNailRules.NailPerStack));
            Assert.Equal(0.1f, RustyNailRules.NailCoefficient(1, RustyNailRules.NailPerStack), 5);
            Assert.Equal(0.102f, RustyNailRules.NailCoefficient(3, RustyNailRules.NailPerStack), 5);
            Assert.Equal(0.11f, RustyNailRules.NailCoefficient(3, RustyNailRules.VoidNailPerStack), 5);
        }

        [Fact]
        public void Burst_OneFiftyPlusFiftyPerExtraCopy()
        {
            Assert.Equal(0f, RustyNailRules.BurstCoefficient(0));
            Assert.Equal(1.5f, RustyNailRules.BurstCoefficient(1), 5);
            Assert.Equal(2.5f, RustyNailRules.BurstCoefficient(3), 5);
        }

        [Fact]
        public void Spread_StopsAtMaxChain()
        {
            Assert.True(RustyNailRules.CanSpread(0, 2));
            Assert.True(RustyNailRules.CanSpread(1, 2));
            Assert.False(RustyNailRules.CanSpread(2, 2));
            Assert.False(RustyNailRules.CanSpread(0, 0)); // 0 = a chuva não marca ninguém
        }
    }
}
