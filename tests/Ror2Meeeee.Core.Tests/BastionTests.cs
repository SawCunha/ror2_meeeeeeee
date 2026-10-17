using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class BastionTests
    {
        [Fact]
        public void ExtraShield_FortyPercentPerCopy()
        {
            Assert.Equal(0f, Bastion.ExtraShield(1000f, 0));
            Assert.Equal(400f, Bastion.ExtraShield(1000f, 1), 3);
            Assert.Equal(4000f, Bastion.ExtraShield(1000f, 10), 3);
        }

        [Fact]
        public void ForceRegen_OnlyAfterDelay_AndWhenNotFull()
        {
            Assert.False(Bastion.ShouldForceRegen(1.9f, 50f, 100f));
            Assert.True(Bastion.ShouldForceRegen(2f, 50f, 100f));
            Assert.False(Bastion.ShouldForceRegen(10f, 100f, 100f));
            Assert.False(Bastion.ShouldForceRegen(10f, 0f, 0f));
        }
    }
}
