using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class PortalBatteryTests
    {
        [Fact]
        public void NoStacks_Neutral() => Assert.Equal(1f, PortalBattery.ChargeRateMultiplier(0));

        [Fact]
        public void TenPercentPerTeamCopy_NoCap()
        {
            Assert.Equal(1.1f, PortalBattery.ChargeRateMultiplier(1), 5);
            Assert.Equal(2f, PortalBattery.ChargeRateMultiplier(10), 5);
            Assert.Equal(11f, PortalBattery.ChargeRateMultiplier(100), 5);
        }
    }
}
