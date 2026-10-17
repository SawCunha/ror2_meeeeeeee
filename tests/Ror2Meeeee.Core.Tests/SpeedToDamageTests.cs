using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class SpeedToDamageTests
    {
        [Fact] public void Ratio_NoStacks_Zero() => Assert.Equal(0f, SpeedToDamage.Ratio(0));
        [Fact] public void Ratio_OneStack_OneToOne() => Assert.Equal(1f, SpeedToDamage.Ratio(1));
        [Fact] public void Ratio_ThreeStacks_Double() => Assert.Equal(2f, SpeedToDamage.Ratio(3));

        [Fact]
        public void Convert_OneStack_FiftyPercentSpeedBecomesFiftyPercentDamage()
        {
            Assert.True(SpeedToDamage.Convert(10.5f, 7f, 12f, 1, out var speed, out var damage));
            Assert.Equal(7f, speed);
            Assert.Equal(18f, damage, 4);
        }

        [Fact]
        public void Convert_TwoStacks_UsesOneAndAHalfRatio()
        {
            SpeedToDamage.Convert(14f, 7f, 10f, 2, out _, out var damage); // +100% velocidade * 1.5
            Assert.Equal(25f, damage, 4);
        }

        [Fact]
        public void Convert_Slowed_Unchanged()
        {
            Assert.False(SpeedToDamage.Convert(4f, 7f, 12f, 1, out var speed, out var damage));
            Assert.Equal(4f, speed);
            Assert.Equal(12f, damage);
        }

        [Fact] public void Convert_ExactlyNeutral_Unchanged() => Assert.False(SpeedToDamage.Convert(7f, 7f, 12f, 1, out _, out _));
        [Fact] public void Convert_NoStacks_Unchanged() => Assert.False(SpeedToDamage.Convert(14f, 7f, 12f, 0, out _, out _));
        [Fact] public void Convert_ZeroNeutral_Unchanged() => Assert.False(SpeedToDamage.Convert(14f, 0f, 12f, 1, out _, out _));
        [Fact] public void Convert_NaNSpeed_Unchanged() => Assert.False(SpeedToDamage.Convert(float.NaN, 7f, 12f, 1, out _, out _));
        [Fact] public void Convert_InfiniteSpeed_Unchanged() => Assert.False(SpeedToDamage.Convert(float.PositiveInfinity, 7f, 12f, 1, out _, out _));
    }
}
