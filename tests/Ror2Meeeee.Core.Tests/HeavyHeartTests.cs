using Ror2Meeeee.Core.Stats;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class HeavyHeartTests
    {
        [Fact]
        public void NoStacks_Neutral()
        {
            Assert.Equal(1f, HeavyHeart.HealthMultiplier(0));
            Assert.Equal(1f, HeavyHeart.DamageTakenMultiplier(0));
        }

        [Fact]
        public void Compound_PerStack()
        {
            Assert.Equal(1.5f, HeavyHeart.HealthMultiplier(1), 5);
            Assert.Equal(1.3f, HeavyHeart.DamageTakenMultiplier(1), 5);
            Assert.Equal(2.25f, HeavyHeart.HealthMultiplier(2), 5);
            Assert.Equal(57.665f, HeavyHeart.HealthMultiplier(10), 2);
            Assert.Equal(13.786f, HeavyHeart.DamageTakenMultiplier(10), 2);
        }

        [Fact]
        public void HugeStacks_StayFinite()
        {
            Assert.False(float.IsInfinity(HeavyHeart.HealthMultiplier(100000)));
            Assert.Equal((float)HeavyHeart.MaxMultiplier, HeavyHeart.HealthMultiplier(100000));
            Assert.False(float.IsInfinity(1000f * HeavyHeart.HealthMultiplier(100000)));
        }

        [Fact]
        public void ClampScale_KeepsHealthPlusShieldUnderIntSafeCap()
        {
            Assert.Equal(1f, StatPool.ClampScale(1000f, 500f));
            var s = StatPool.ClampScale(3e9f, 1e9f);
            Assert.True((3e9f + 1e9f) * s <= StatPool.MaxCombined * 1.0001f);
            Assert.True((3e9f + 1e9f) * s < int.MaxValue);
        }

        [Fact]
        public void StatPoolAdjust_GainsIncreaseAndClampsToMax()
        {
            Assert.Equal(150f, StatPool.Adjust(100f, 100f, 150f), 4); // pegou o item cheio: continua cheio
            Assert.Equal(90f, StatPool.Adjust(40f, 100f, 150f), 4);   // ganha os +50
            Assert.Equal(100f, StatPool.Adjust(140f, 150f, 100f), 4); // perdeu o item: corta no máximo
            Assert.Equal(80f, StatPool.Adjust(80f, 150f, 150f), 4);   // recálculo comum: nada muda
        }
    }
}
