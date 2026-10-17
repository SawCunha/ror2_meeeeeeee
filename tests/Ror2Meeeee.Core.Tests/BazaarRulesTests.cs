using Ror2Meeeee.Core.Bazaar;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class BazaarRulesTests
    {
        [Fact]
        public void PickWeighted_AllZero_ReturnsMinusOne()
        {
            Assert.Equal(-1, BazaarRules.PickWeighted(new[] { 0f, 0f, -1f, float.NaN }, 0.5));
        }

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(0.54, 0)]
        [InlineData(0.56, 1)]
        [InlineData(0.84, 1)]
        [InlineData(0.86, 3)]
        [InlineData(0.9999999, 3)]
        public void PickWeighted_PicksBucketByRoll_SkippingZeroWeights(double roll, int expected)
        {
            // 0.55 / 0.30 / 0 / 0.15 (o tier de peso 0 nunca sai)
            Assert.Equal(expected, BazaarRules.PickWeighted(new[] { 0.55f, 0.3f, 0f, 0.15f }, roll));
        }

        [Fact]
        public void PickWeighted_InvalidWeightsAreIgnored()
        {
            Assert.Equal(2, BazaarRules.PickWeighted(new[] { -5f, float.NaN, 1f }, 0.0));
        }

        [Fact]
        public void ScaledCost_CoefficientOne_IsBaseTimesMultiplier()
        {
            Assert.Equal(50, BazaarRules.GoldCost(1f, 2f));
            Assert.Equal(400, BazaarRules.GoldCost(1f, 16f));
        }

        [Fact]
        public void ScaledCost_MatchesGameFormula()
        {
            // jogo: (int)(25 * 2^1.25) = 59
            Assert.Equal(59, BazaarRules.ScaledCost(25, 2f, 1f));
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-3f)]
        [InlineData(float.NaN)]
        public void ScaledCost_InvalidCoefficient_TreatedAsOne(float coef)
        {
            Assert.Equal(25, BazaarRules.GoldCost(coef, 1f));
        }

        [Fact]
        public void ScaledCost_NegativeMultiplier_IsFree()
        {
            Assert.Equal(0, BazaarRules.GoldCost(3f, -1f));
        }

        [Fact]
        public void ScaledCost_Huge_SaturatesAtIntMax()
        {
            Assert.Equal(int.MaxValue, BazaarRules.GoldCost(1e6f, 1e6f));
        }

        [Theory]
        [InlineData(0, -1, true)]
        [InlineData(999, -1, true)]
        [InlineData(0, 0, false)]
        [InlineData(2, 3, true)]
        [InlineData(3, 3, false)]
        public void UnderLimit_MinusOneIsUnlimited(int used, int limit, bool expected)
        {
            Assert.Equal(expected, BazaarRules.UnderLimit(used, limit));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-4, 1)]
        [InlineData(4, 4)]
        [InlineData(9, 6)]
        public void ClampChests_StaysInsideFixedPositions(int n, int expected)
        {
            Assert.Equal(expected, BazaarRules.ClampChests(n));
        }

        [Fact]
        public void AddMoney_Saturates()
        {
            Assert.Equal(uint.MaxValue, BazaarRules.AddMoney(uint.MaxValue - 1, 10));
            Assert.Equal(0u, BazaarRules.AddMoney(5, -10));
            Assert.Equal(15u, BazaarRules.AddMoney(5, 10));
        }
    }
}
