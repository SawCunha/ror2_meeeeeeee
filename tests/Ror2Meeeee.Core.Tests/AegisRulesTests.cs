using Ror2Meeeee.Core.Aegis;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class AegisRulesTests
    {
        [Fact]
        public void NoStacks_Nothing()
        {
            Assert.Equal(0f, AegisRules.StealFraction(0));
            Assert.Equal(0f, AegisRules.VoidFraction(0));
            Assert.Equal(0f, AegisRules.VoidRadius(0));
        }

        [Fact]
        public void Green_EightPercentPerCopy_CappedAtHalf()
        {
            Assert.Equal(0.08f, AegisRules.StealFraction(1), 5);
            Assert.Equal(0.24f, AegisRules.StealFraction(3), 5);
            Assert.Equal(0.5f, AegisRules.StealFraction(10), 5); // x10 do multiplicador: 80% -> teto
        }

        [Fact]
        public void Void_ThreePercentPerCopy_CappedAtTwenty()
        {
            Assert.Equal(0.03f, AegisRules.VoidFraction(1), 5);
            Assert.Equal(0.2f, AegisRules.VoidFraction(10), 5);
        }

        [Fact]
        public void Void_RadiusThirteenPlusTwoPerExtra_CappedAtThirty()
        {
            Assert.Equal(13f, AegisRules.VoidRadius(1), 5);
            Assert.Equal(17f, AegisRules.VoidRadius(3), 5);
            Assert.Equal(30f, AegisRules.VoidRadius(10), 5);
        }
    }
}
