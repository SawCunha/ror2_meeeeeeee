using Ror2Meeeee.Core.Tempest;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class OverloadGovernorTests
    {
        private static OverloadGovernor Make()
        {
            var g = new OverloadGovernor();
            g.Configure(enterMs: 33f, enterSeconds: 10f, exitMs: 25f, exitSeconds: 20f, recoverySeconds: 30f);
            return g;
        }

        private static void Run(OverloadGovernor g, float ms, int seconds)
        {
            for (var i = 0; i < seconds; i++) g.Update(ms, 1f);
        }

        [Fact]
        public void Enters_OnlyAfterPersistence()
        {
            var g = Make();
            Run(g, 50f, 9);
            Assert.Equal(OverloadState.Normal, g.State);
            Run(g, 50f, 1);
            Assert.Equal(OverloadState.Reduced, g.State);
            Assert.Equal(0f, g.Level);
        }

        [Fact]
        public void NoiseBetweenThresholds_DoesNotOscillate()
        {
            var g = Make();
            Run(g, 50f, 10);
            Run(g, 30f, 100); // entre saída (25) e entrada (33): segue reduzido
            Assert.Equal(OverloadState.Reduced, g.State);
        }

        [Fact]
        public void SpikeShorterThanWindow_Ignored()
        {
            var g = Make();
            for (var i = 0; i < 50; i++) { Run(g, 50f, 5); Run(g, 20f, 1); }
            Assert.Equal(OverloadState.Normal, g.State);
        }

        [Fact]
        public void Recovers_LinearlyThenNormal()
        {
            var g = Make();
            Run(g, 50f, 10);
            Run(g, 20f, 20);
            Assert.Equal(OverloadState.Recovering, g.State);
            Run(g, 20f, 15);
            Assert.Equal(0.5f, g.Level, 3);
            Run(g, 20f, 15);
            Assert.Equal(OverloadState.Normal, g.State);
            Assert.Equal(1f, g.Level);
        }

        [Fact]
        public void Reentry_DuringRecovery()
        {
            var g = Make();
            Run(g, 50f, 10);
            Run(g, 20f, 25);
            Run(g, 50f, 10);
            Assert.Equal(OverloadState.Reduced, g.State);
        }

        [Fact]
        public void ExitNotBelowEnter_IsForcedBelow()
        {
            var g = new OverloadGovernor();
            g.Configure(30f, 1f, 40f, 1f, 1f);
            Assert.Equal(22.5f, g.ExitMs);
        }
    }
}
