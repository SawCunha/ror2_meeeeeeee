using Ror2Meeeee.Core.Limits;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class StackPolicyTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(int.MaxValue)]
        public void Off_ReturnsOriginal(int stack)
        {
            Assert.Equal(stack, StackPolicy.Off.Apply(stack));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]   // no limite
        [InlineData(11, 10)]   // acima
        [InlineData(int.MaxValue, 10)]
        public void Clamp_LimitsStack(int stack, int expected)
        {
            Assert.Equal(expected, StackPolicy.Clamp(10).Apply(stack));
        }

        [Fact]
        public void Clamp_NegativeStack_ReturnsZero()
        {
            Assert.Equal(0, StackPolicy.Clamp(10).Apply(-5));
        }

        [Fact]
        public void Clamp_LimitBelowOne_IsOne()
        {
            Assert.Equal(1, StackPolicy.Clamp(0).Apply(5));
            Assert.Equal(1, StackPolicy.Clamp(-3).Apply(5));
        }

        [Fact]
        public void Create_ByMode()
        {
            Assert.Same(StackPolicy.Off, StackPolicy.Create(StackMode.Off, 10));
            Assert.Equal(10, StackPolicy.Create(StackMode.Clamp, 10).Limit);
            Assert.Equal(10, StackPolicy.Create(StackMode.Aggregate, 10).Limit);
        }

        [Fact]
        public void Clamp_ExposesLimit_AndOffHasNone()
        {
            Assert.Equal(10, StackPolicy.Clamp(10).Limit);
            Assert.Equal(int.MaxValue, StackPolicy.Off.Limit);
        }
    }
}
