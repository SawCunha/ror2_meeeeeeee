using Ror2Meeeee.Core.Helluva;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class HelluvaRulesTests
    {
        // ---- Grimório ----
        [Fact] public void GrimoireCredit_ScalesWithDifficulty() => Assert.Equal(300f, HelluvaRules.GrimoireCredit(100f, 3f));
        [Fact] public void GrimoireCredit_DifficultyBelowOne_TreatedAsOne() => Assert.Equal(100f, HelluvaRules.GrimoireCredit(100f, 0.5f));
        [Fact] public void GrimoireCredit_ZeroBase_None() => Assert.Equal(0f, HelluvaRules.GrimoireCredit(0f, 3f));
        [Fact] public void GrimoireCredit_NaN_None() => Assert.Equal(0f, HelluvaRules.GrimoireCredit(float.NaN, 3f));
        [Fact] public void GrimoireCredit_Huge_Capped() => Assert.Equal(1e7f, HelluvaRules.GrimoireCredit(1e9f, 1e9f));

        [Fact] public void GrimoireGuarded_CapsPerUse() => Assert.Equal(600f, HelluvaRules.GrimoireCreditGuarded(150f, 10f, 600f, 10, 80));
        [Fact] public void GrimoireGuarded_BelowCap_Unchanged() => Assert.Equal(300f, HelluvaRules.GrimoireCreditGuarded(100f, 3f, 600f, 10, 80));
        [Fact] public void GrimoireGuarded_TooManyMonsters_NoCredit() => Assert.Equal(0f, HelluvaRules.GrimoireCreditGuarded(150f, 10f, 600f, 80, 80));
        [Fact] public void GrimoireGuarded_ZeroLimits_Disabled() => Assert.Equal(1500f, HelluvaRules.GrimoireCreditGuarded(150f, 10f, 0f, 500, 0));

        // ---- Contrato ----
        [Fact] public void ContractGold_OneStack_DoublesReward() => Assert.Equal(50u, HelluvaRules.ContractGold(50, 1));
        [Fact] public void ContractGold_ThreeStacks_Triple() => Assert.Equal(150u, HelluvaRules.ContractGold(50, 3));
        [Fact] public void ContractGoldCapped_AboveMax_Max() => Assert.Equal(20000u, HelluvaRules.ContractGoldCapped(50000, 2, 20000));
        [Fact] public void ContractGoldCapped_BelowMax_Unchanged() => Assert.Equal(300u, HelluvaRules.ContractGoldCapped(100, 3, 20000));
        [Fact] public void ContractGoldCapped_ZeroMax_NoCap() => Assert.Equal(150u, HelluvaRules.ContractGoldCapped(50, 3, 0));
        [Fact] public void ContractGold_NoStacks_None() => Assert.Equal(0u, HelluvaRules.ContractGold(50, 0));
        [Fact] public void ContractGold_Huge_Saturates() => Assert.Equal(uint.MaxValue, HelluvaRules.ContractGold(uint.MaxValue, 5));

        // ---- Ressurreição ----
        [Fact]
        public void ReviveStep_ContinuousNear_CompletesAtRequired()
        {
            var p = 0f;
            Assert.False(HelluvaRules.ReviveStep(ref p, true, 1f, 3f));
            Assert.False(HelluvaRules.ReviveStep(ref p, true, 1f, 3f));
            Assert.True(HelluvaRules.ReviveStep(ref p, true, 1f, 3f));
            Assert.Equal(0f, p);
        }

        [Fact]
        public void ReviveStep_AllyLeaves_Resets()
        {
            var p = 2.5f;
            Assert.False(HelluvaRules.ReviveStep(ref p, false, 1f, 3f));
            Assert.Equal(0f, p);
        }

        [Fact] public void ReviverCost_HalfOfCurrent() => Assert.Equal(50f, HelluvaRules.ReviverCost(100f, 0.5f));
        [Fact] public void ReviverCost_NeverLethal() => Assert.Equal(99f, HelluvaRules.ReviverCost(100f, 1f));
        [Fact] public void ReviverCost_AtOneHealth_None() => Assert.Equal(0f, HelluvaRules.ReviverCost(1f, 0.5f));
        [Fact] public void ReviverCost_NaN_None() => Assert.Equal(0f, HelluvaRules.ReviverCost(float.NaN, 0.5f));
    }
}
