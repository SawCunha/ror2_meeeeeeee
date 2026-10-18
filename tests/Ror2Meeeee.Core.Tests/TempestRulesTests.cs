using Ror2Meeeee.Core.Tempest;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class TempestRulesTests
    {
        private static readonly int[] Steps = TempestRules.DefaultSteps;

        [Fact]
        public void Curve_StartOfRun_IsOne()
        {
            Assert.Equal(1f, new TempestCurve(0.02f, 0.1f, 3f).Evaluate(0f, 0));
        }

        [Fact]
        public void Curve_GrowsByMinuteAndStage_UpToMax()
        {
            var c = new TempestCurve(0.02f, 0.1f, 3f);
            Assert.Equal(1.4f, c.Evaluate(10f, 2), 4);   // 1 + 0.2 + 0.2
            Assert.Equal(3f, c.Evaluate(1000f, 50));
        }

        [Fact]
        public void Curve_NoLoopTerm_StraightLineAcrossLoops()
        {
            var c = new TempestCurve(0f, 0.1f, 100f);
            Assert.Equal(c.Evaluate(0f, 10) - c.Evaluate(0f, 5), c.Evaluate(0f, 5) - c.Evaluate(0f, 0), 4);
        }

        [Fact]
        public void Clamp_NaNGoesToDefault_OutOfRangeToNearestLimit()
        {
            Assert.Equal(3f, TempestRules.Clamp(float.NaN, 3f, 0f, 10f, out var c1));
            Assert.True(c1);
            Assert.Equal(10f, TempestRules.Clamp(50f, 3f, 0f, 10f, out _));
            Assert.Equal(0f, TempestRules.Clamp(-1f, 3f, 0f, 10f, out _));
            Assert.Equal(5f, TempestRules.Clamp(5f, 3f, 0f, 10f, out var c2));
            Assert.False(c2);
        }

        [Theory]
        [InlineData(0, 0f)]
        [InlineData(1, 0.2f)]
        [InlineData(4, 0.7f)]
        [InlineData(5, 0.9f)]
        [InlineData(6, 0.99f)]
        [InlineData(30, 0.99f)]
        public void Reduction_FollowsTableAndHoldsLastValue(int stagesCleared, float expected)
        {
            Assert.Equal(expected, TempestRules.Reduction(stagesCleared, Steps), 4);
        }

        [Theory]
        [InlineData(1, 10f, 0.5f, 0.747f)]  // 0,80 / 3,162
        [InlineData(2, 10f, 0.5f, 0.810f)]  // 0,60 / 3,162
        [InlineData(1, 2f, 0.5f, 0.434f)]   // 0,80 / 1,414
        [InlineData(6, 10f, 0.5f, 0.99f)]   // teto
        [InlineData(0, 10f, 0.5f, 0f)]      // estágio 1 sem redução
        [InlineData(1, 1f, 0.5f, 0.2f)]     // sem Multiplicador
        [InlineData(1, 10f, 0f, 0.2f)]      // expoente 0 desliga
        public void Reduction_WithMultiplier_DividesDamageThatPasses(int stagesCleared, float multiplier, float exponent, float expected)
        {
            Assert.Equal(expected, TempestRules.Reduction(stagesCleared, Steps, multiplier, exponent), 3);
        }

        [Theory]
        [InlineData(10f, 0.5f, 3.162f)]
        [InlineData(2f, 1f, 2f)]
        [InlineData(1f, 0.5f, 1f)]
        [InlineData(10f, 0f, 1f)]
        public void MultiplierFactor_IsPowerOrOne(float multiplier, float exponent, float expected)
        {
            Assert.Equal(expected, TempestRules.MultiplierFactor(multiplier, exponent), 3);
        }

        [Theory]
        [InlineData(false, false, 1)]
        [InlineData(false, true, 2)]
        [InlineData(true, false, 0)]
        [InlineData(true, true, 0)] // chefe elite continua sem
        public void ExtraLives_NormalEliteBoss(bool boss, bool elite, int expected)
        {
            Assert.Equal(expected, TempestRules.ExtraLives(boss, elite, 1, 2));
        }

        [Fact]
        public void ParseSteps_DefaultText_HasNoCorrection()
        {
            Assert.Equal(Steps, TempestRules.ParseSteps("20,40,55,70,90,99", out var corrected));
            Assert.False(corrected);
        }

        [Fact]
        public void ParseSteps_AboveCapAndDecreasing_AreCorrected()
        {
            Assert.Equal(new[] { 50, 50, 99 }, TempestRules.ParseSteps("50, 30, 150", out var corrected));
            Assert.True(corrected);
        }

        [Fact]
        public void ParseSteps_NonNumeric_FallsBackToDefault()
        {
            Assert.Equal(Steps, TempestRules.ParseSteps("20,abc", out var corrected));
            Assert.True(corrected);
        }

        [Fact]
        public void ParseSteps_Empty_MeansNoReduction()
        {
            var steps = TempestRules.ParseSteps("", out _);
            Assert.Equal(0f, TempestRules.Reduction(10, steps));
        }

        [Fact]
        public void HealthStacks_OnePercentPerStack()
        {
            Assert.Equal(55, TempestRules.HealthStacks(0.55f));
            Assert.Equal(99, TempestRules.HealthStacks(0.99f));
            Assert.Equal(99, TempestRules.HealthStacks(5f));
            Assert.Equal(0, TempestRules.HealthStacks(-1f));
        }

        [Fact]
        public void BoostStacks_TenPercentSteps()
        {
            Assert.Equal(0, TempestRules.BoostStacks(1f));
            Assert.Equal(3, TempestRules.BoostStacks(1.25f)); // arredonda para o passo mais próximo (2,5 -> 3)
            Assert.Equal(20, TempestRules.BoostStacks(3f));
            Assert.Equal(0, TempestRules.BoostStacks(float.NaN));
        }

        [Fact]
        public void EliteBias_InverseOfMultiplier_NeverZero()
        {
            Assert.Equal(1f, TempestRules.EliteBias(1f));
            Assert.Equal(0.5f, TempestRules.EliteBias(2f));
            Assert.Equal(1f, TempestRules.EliteBias(0f));
        }

        [Theory]
        [InlineData(1, 60)]
        [InlineData(2, 70)]
        [InlineData(4, 90)]
        [InlineData(20, 120)]
        public void Capacity_BasePlusExtraPlayers_Clamped(int players, int expected)
        {
            Assert.Equal(expected, TempestRules.Capacity(players, 60, 10, 120));
        }

        [Fact]
        public void Capacity_NeverBelowGameLimit()
        {
            Assert.Equal(40, TempestRules.Capacity(1, 10, 0, 20));
        }

        [Fact]
        public void Gate_ClosesBeforeCap_RampsBackWithoutBurst()
        {
            Assert.Equal(0f, TempestRules.Gate(1f, 60, 55, 5, 10f, 1f));   // 5 vagas = só a folga: fecha
            var g = TempestRules.Gate(0f, 60, 30, 5, 10f, 1f);
            Assert.Equal(0.1f, g, 4);                                        // reabre 10% por segundo
            Assert.Equal(0.2f, TempestRules.Gate(g, 60, 30, 5, 10f, 1f), 4);
            Assert.Equal(0.4f, TempestRules.Gate(1f, 60, 53, 5, 10f, 1f), 4); // 2 vagas livres de 5: no máximo 40%
        }

        [Fact]
        public void Pressure_StartsAfterDelay_Capped()
        {
            Assert.Equal(0f, TempestRules.Pressure(200f, 300f, 0.1f, 1f));
            Assert.Equal(0.1f, TempestRules.Pressure(360f, 300f, 0.1f, 1f), 4);
            Assert.Equal(1f, TempestRules.Pressure(100000f, 300f, 0.1f, 1f));
        }

        [Fact]
        public void Credit_ZeroWithoutGatePerfOrExtraSpawn()
        {
            Assert.Equal(0f, TempestRules.Credit(1f, 2f, 1, 2f, 0f, 1f));
            Assert.Equal(0f, TempestRules.Credit(1f, 2f, 1, 2f, 1f, 0f));
            Assert.Equal(0f, TempestRules.Credit(1f, 2f, 1, 1f, 1f, 1f));
        }

        [Fact]
        public void Credit_MatchesGameFormulaShape()
        {
            // 2 * (1 + 0.4*5) * (0.5 + 0.5*3) * (1.5 - 1) = 2 * 3 * 2 * 0.5
            Assert.Equal(6f, TempestRules.Credit(2f, 5f, 3, 1.5f, 1f, 1f), 4);
        }

        [Theory]
        [InlineData(0f, true)]
        [InlineData(44f, true)]
        [InlineData(45f, false)]
        [InlineData(119f, false)]
        [InlineData(120f, true)]
        public void IsSwarm_AlternatesStartingWithSwarm(float t, bool expected)
        {
            Assert.Equal(expected, TempestRules.IsSwarm(t, 45f, 75f));
        }

        [Fact]
        public void IsSwarm_ZeroSeconds_Disabled()
        {
            Assert.False(TempestRules.IsSwarm(0f, 0f, 75f));
        }

        [Theory]
        [InlineData(10, 10)]
        [InlineData(12, 10)]
        [InlineData(7, 5)]
        [InlineData(0, 5)]
        public void SanitizeFinalStage_MultipleOfFive(int input, int expected)
        {
            Assert.Equal(expected, TempestRules.SanitizeFinalStage(input, out _));
        }

        [Theory]
        [InlineData(4, 10, true)]   // estágio 5
        [InlineData(9, 10, false)]  // estágio 10
        [InlineData(14, 10, false)]
        [InlineData(4, 5, false)]   // 5 = jogo original
        public void FinalLocked_UntilFinalStage(int stagesCleared, int finalStage, bool expected)
        {
            Assert.Equal(expected, TempestRules.FinalLocked(stagesCleared, finalStage));
        }
    }
}
