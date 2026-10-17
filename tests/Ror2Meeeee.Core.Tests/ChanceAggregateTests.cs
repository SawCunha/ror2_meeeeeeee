using Ror2Meeeee.Core.Limits;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class ChanceAggregateTests
    {
        [Theory]
        [InlineData(3, 3, 1f)]     // sem corte
        [InlineData(0, 0, 1f)]
        [InlineData(10, 5, 2f)]    // 40% -> 20%: cada pacote vale 2x
        [InlineData(25, 5, 5f)]    // 100% -> 20%
        [InlineData(100, 5, 5f)]   // acima de 25 stacks o jogo já está em 100%
        [InlineData(100, 25, 1f)]  // teto do jogo == teto do limite
        public void Factor_PreservesExpectedValue(int stack, int capped, float expected)
        {
            Assert.Equal(expected, ChanceAggregate.Factor(stack, capped, 4f), 3);
        }
    }
}
