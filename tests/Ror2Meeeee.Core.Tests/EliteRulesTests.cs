using Ror2Meeeee.Core.Elites;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class EliteRulesTests
    {
        [Fact]
        public void PickTwoDistinct_AllDraws_NeverRepeatAndStayInRange()
        {
            for (var a = 0; a < 4; a++)
            for (var b = 0; b < 3; b++)
            {
                var (first, second) = EliteRules.PickTwoDistinct(4, a, b);
                Assert.NotEqual(first, second);
                Assert.InRange(first, 0, 3);
                Assert.InRange(second, 0, 3);
            }
        }

        [Fact]
        public void PickTwoDistinct_OneOrNone_ReturnsMinusOne()
        {
            Assert.Equal((0, -1), EliteRules.PickTwoDistinct(1, 0, 0));
            Assert.Equal((-1, -1), EliteRules.PickTwoDistinct(0, 0, 0));
        }
    }
}
