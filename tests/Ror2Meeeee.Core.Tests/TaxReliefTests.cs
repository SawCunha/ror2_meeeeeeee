using Ror2Meeeee.Core.Tax;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class TaxReliefTests
    {
        [Fact] public void NoItems_FullTax() => Assert.Equal(1f, TaxRelief.Multiplier(false, false, false));
        [Fact] public void White_Reduces20() => Assert.Equal(0.8f, TaxRelief.Multiplier(true, false, false));
        [Fact] public void Green_Reduces30() => Assert.Equal(0.7f, TaxRelief.Multiplier(false, true, false));
        [Fact] public void Red_Reduces50() => Assert.Equal(0.5f, TaxRelief.Multiplier(false, false, true));
        [Fact] public void WhiteAndGreen_Reduces50() => Assert.Equal(0.5f, TaxRelief.Multiplier(true, true, false));
        [Fact] public void AllThree_AnnulsTax() => Assert.Equal(0f, TaxRelief.Multiplier(true, true, true));

        [Fact] public void Gold_NoItems_Unchanged() => Assert.Equal(100u, TaxRelief.Gold(100, false, false, false));
        [Fact] public void Gold_White_Plus10() => Assert.Equal(110u, TaxRelief.Gold(100, true, false, false));
        [Fact] public void Gold_Green_Plus20() => Assert.Equal(120u, TaxRelief.Gold(100, false, true, false));
        [Fact] public void Gold_Red_Plus30() => Assert.Equal(130u, TaxRelief.Gold(100, false, false, true));
        [Fact] public void Gold_AllThree_Plus60() => Assert.Equal(160u, TaxRelief.Gold(100, true, true, true));
        [Fact] public void Gold_Zero_StaysZero() => Assert.Equal(0u, TaxRelief.Gold(0, true, true, true));
        [Fact] public void Gold_SmallAmount_RoundsHalfUp() => Assert.Equal(6u, TaxRelief.Gold(5, true, false, false)); // 5.5 -> 6
        [Fact] public void Gold_SmallAmount_RoundsDown() => Assert.Equal(4u, TaxRelief.Gold(4, true, false, false)); // 4.4 -> 4
        [Fact] public void Gold_Huge_DoesNotOverflow() => Assert.Equal(uint.MaxValue, TaxRelief.Gold(uint.MaxValue, true, true, true));
    }
}
