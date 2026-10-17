using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class MultiShotTests
    {
        [Fact] public void ExtraShots_NoStacks_Zero() => Assert.Equal(0, MultiShot.ExtraShots(0, 5));
        [Fact] public void ExtraShots_OnePerStack() => Assert.Equal(3, MultiShot.ExtraShots(3, 5));
        [Fact] public void ExtraShots_CappedAtMax() => Assert.Equal(5, MultiShot.ExtraShots(40, 5));
        [Fact] public void ExtraShots_ZeroMax_Off() => Assert.Equal(0, MultiShot.ExtraShots(3, 0));
        [Fact] public void ExtraShots_NegativeMax_Off() => Assert.Equal(0, MultiShot.ExtraShots(3, -1));

        [Theory]
        [InlineData(0, 5f)] [InlineData(1, -5f)] [InlineData(2, 10f)] [InlineData(3, -10f)] [InlineData(4, 15f)]
        public void FanAngle_AlternatesAndOpens(int index, float expected) => Assert.Equal(expected, MultiShot.FanAngle(index, 5f));

        [Fact] public void FanAngle_NegativeIndex_Center() => Assert.Equal(0f, MultiShot.FanAngle(-1, 5f));

        [Fact] public void Budget_UnderLimit_AllowsAll() => Assert.Equal(3, MultiShot.Budget(3, 10, 64));
        [Fact] public void Budget_NearLimit_Trims() => Assert.Equal(2, MultiShot.Budget(5, 62, 64));
        [Fact] public void Budget_AtLimit_None() => Assert.Equal(0, MultiShot.Budget(5, 64, 64));
        [Fact] public void Budget_NothingWanted_None() => Assert.Equal(0, MultiShot.Budget(0, 0, 64));
    }
}
