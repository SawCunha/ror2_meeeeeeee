using Ror2Meeeee.Core.Difficulty;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class DifficultyCurveTests
    {
        private static readonly DifficultyGains Gains = new DifficultyGains(health: 1f, damage: 0.5f, quantity: 1f, pace: 0.5f);

        [Fact]
        public void Factor1_IsIdentityOnEveryAxis()
        {
            var m = DifficultyCurve.Evaluate(1f, Gains);
            Assert.Equal(1f, m.Health);
            Assert.Equal(1f, m.Damage);
            Assert.Equal(1f, m.Quantity);
            Assert.Equal(1f, m.Pace);
        }

        [Fact]
        public void FactorAboveOne_ScalesLinearlyByGain()
        {
            var m = DifficultyCurve.Evaluate(3f, Gains);
            Assert.Equal(3f, m.Health);     // 1 + 2*1.0
            Assert.Equal(2f, m.Damage);     // 1 + 2*0.5
            Assert.Equal(3f, m.Quantity);
            Assert.Equal(2f, m.Pace);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-2f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void InvalidFactor_FallsBackToDefault(float bad)
        {
            Assert.Equal(DifficultyCurve.DefaultFactor, DifficultyCurve.SanitizeFactor(bad));
            Assert.Equal(1f, DifficultyCurve.Evaluate(bad, Gains).Quantity);
        }

        [Fact]
        public void ExtremeFactor_IsClampedWithoutOverflow()
        {
            var m = DifficultyCurve.Evaluate(float.MaxValue, Gains);
            Assert.True(m.Health <= DifficultyCurve.MaxMultiplier);
            Assert.False(float.IsInfinity(m.Health));
        }

        [Fact]
        public void FactorBelowOne_NeverGoesToZeroOrNegative()
        {
            var m = DifficultyCurve.Evaluate(0.01f, new DifficultyGains(5f, 5f, 5f, 5f));
            Assert.True(m.Health >= DifficultyCurve.MinMultiplier);
            Assert.True(m.Quantity >= DifficultyCurve.MinMultiplier);
        }

        [Theory]
        [InlineData(1f, 0f, 1f)]
        [InlineData(1f, 50f, 1.5f)]
        [InlineData(2f, 50f, 3f)]      // multiplica com o fator
        [InlineData(1f, -50f, 0.5f)]
        [InlineData(1f, -500f, DifficultyCurve.MinMultiplier)]
        [InlineData(1f, float.NaN, 1f)]
        [InlineData(1f, float.MaxValue, DifficultyCurve.MaxMultiplier)]
        public void WithBonus_CombinesPercentOnTopOfMultiplier(float mult, float percent, float expected)
        {
            Assert.Equal(expected, DifficultyCurve.WithBonus(mult, percent), 4);
        }

        [Theory]
        [InlineData(0, 1f)]
        [InlineData(-3, 1f)]
        [InlineData(1, 1.25f)]
        [InlineData(4, 2f)]
        [InlineData(1000, 10f)]            // saturado em Max
        [InlineData(int.MaxValue, 10f)]
        public void Horde_QuantityBySaturatingStacks(int stacks, float expected)
        {
            Assert.Equal(expected, HordeCurve.QuantityMultiplier(stacks, 0.25f, 10f), 4);
        }

        [Fact]
        public void Horde_InvalidParams_AreNeutral()
        {
            Assert.Equal(1f, HordeCurve.QuantityMultiplier(5, float.NaN, 10f));
            Assert.Equal(1f, HordeCurve.QuantityMultiplier(5, -1f, 10f));
            Assert.Equal(1f, HordeCurve.QuantityMultiplier(5, 0.25f, 0.5f)); // max < 1 => sem efeito
        }
    }
}
