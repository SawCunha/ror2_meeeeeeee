using Ror2Meeeee.Core.Helluva;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class DropRulesTests
    {
        [Fact] public void Clamp_NoItems_Zero() => Assert.Equal(0, DropRules.ClampCount(5, 0, 10));
        [Fact] public void Clamp_RequestAboveOwned_Owned() => Assert.Equal(3, DropRules.ClampCount(9, 3, 10));
        [Fact] public void Clamp_RequestAboveMax_Max() => Assert.Equal(10, DropRules.ClampCount(50, 40, 10));
        [Fact] public void Clamp_ZeroOrNegativeRequest_One() => Assert.Equal(1, DropRules.ClampCount(0, 5, 10));
        [Fact] public void Clamp_MaxZero_Disabled() => Assert.Equal(0, DropRules.ClampCount(1, 5, 0));
    }
}
