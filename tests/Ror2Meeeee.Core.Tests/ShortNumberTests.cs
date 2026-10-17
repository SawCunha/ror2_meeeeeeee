using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class ShortNumberTests
    {
        [Theory]
        [InlineData(0, "0")]
        [InlineData(950, "950")]
        [InlineData(12345, "12,3 mil")]
        [InlineData(4_500_000, "4,5 mi")]
        [InlineData(1_200_000_000, "1,2 bi")]
        [InlineData(3.4e12, "3,4 tri")]
        [InlineData(-2000, "-2 mil")]
        public void Formats(double value, string expected) => Assert.Equal(expected, ShortNumber.Format(value));
    }
}
