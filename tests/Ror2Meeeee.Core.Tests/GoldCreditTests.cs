using Ror2Meeeee.Core.Gold;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class GoldCreditTests
    {
        [Fact] public void OneStackRatioOne_AddsSameAmount() => Assert.Equal(25f, GoldCredit.Compute(25, 1, 1f));
        [Fact] public void ScalesWithStacksAndRatio() => Assert.Equal(60f, GoldCredit.Compute(10, 3, 2f));
        [Fact] public void NoItem_IsZero() => Assert.Equal(0f, GoldCredit.Compute(100, 0, 1f));
        [Fact] public void ZeroGold_IsZero() => Assert.Equal(0f, GoldCredit.Compute(0, 5, 1f));
        [Theory]
        [InlineData(0f)] [InlineData(-1f)] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)]
        public void InvalidRatio_IsZero(float ratio) => Assert.Equal(0f, GoldCredit.Compute(10, 1, ratio));
        [Fact] public void Huge_DoesNotOverflow() => Assert.False(float.IsInfinity(GoldCredit.Compute(uint.MaxValue, int.MaxValue, 1e10f)));
    }
}
