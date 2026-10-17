using Ror2Meeeee.Core.Limits;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class ProjectileBudgetTests
    {
        [Fact]
        public void UnderCap_FiresWithOwnDamage()
        {
            var w = new BudgetWindow();
            Assert.True(ProjectileBudget.Decide(ref w, 0f, 3, 10f, out var d));
            Assert.Equal(10f, d);
        }

        [Fact]
        public void OverCap_SkipsAndCarriesDamageToNextAllowed()
        {
            var w = new BudgetWindow();
            for (var i = 0; i < 2; i++) ProjectileBudget.Decide(ref w, 0f, 2, 10f, out _);
            Assert.False(ProjectileBudget.Decide(ref w, 0.5f, 2, 10f, out _));
            Assert.False(ProjectileBudget.Decide(ref w, 0.6f, 2, 10f, out _));
            Assert.True(ProjectileBudget.Decide(ref w, 1.1f, 2, 10f, out var d)); // nova janela
            Assert.Equal(30f, d); // 10 próprio + 20 guardados: dano total preservado
        }

        [Fact]
        public void ZeroCap_MeansNoLimit()
        {
            var w = new BudgetWindow();
            for (var i = 0; i < 1000; i++) Assert.True(ProjectileBudget.Decide(ref w, 0f, 0, 1f, out _));
        }
    }
}
