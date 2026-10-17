using Ror2Meeeee.Core.Measurement;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class FrameStatsTests
    {
        private static (float mean, float p95) Run(float[] samples, int count)
        {
            FrameStats.Compute(samples, count, new float[samples.Length], out var mean, out var p95);
            return (mean, p95);
        }

        [Fact]
        public void EmptyBuffer_IsZero()
        {
            var (mean, p95) = Run(new float[8], 0);
            Assert.Equal(0f, mean);
            Assert.Equal(0f, p95);
        }

        [Fact]
        public void SingleSample_IsThatSample()
        {
            var (mean, p95) = Run(new[] { 16.5f }, 1);
            Assert.Equal(16.5f, mean);
            Assert.Equal(16.5f, p95);
        }

        [Fact]
        public void FullBuffer_NearestRankP95()
        {
            var s = new float[100];
            for (var i = 0; i < 100; i++) s[100 - 1 - i] = i + 1; // 100..1, fora de ordem
            var (mean, p95) = Run(s, 100);
            Assert.Equal(50.5f, mean);
            Assert.Equal(95f, p95);
        }

        [Fact]
        public void RepeatedValues()
        {
            var (mean, p95) = Run(new[] { 10f, 10f, 10f, 10f }, 4);
            Assert.Equal(10f, mean);
            Assert.Equal(10f, p95);
        }

        [Fact]
        public void CountSmallerThanBuffer_IgnoresTail()
        {
            var (mean, p95) = Run(new[] { 1f, 3f, 999f, 999f }, 2);
            Assert.Equal(2f, mean);
            Assert.Equal(3f, p95);
        }
    }
}
