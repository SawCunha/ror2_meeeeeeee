using Ror2Meeeee.Core.Progression;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class ProgressionTests
    {
        private static readonly ScorePars Pars = new ScorePars(240f, 1f, 0.5f, 20f);
        private static readonly LadderRules Rules = new LadderRules(0.5f, 0.25f, 2f); // níveis 0..4

        private static RunStats Stats(int players = 2, double seconds = 600, int charged = 2, double chargeSeconds = 480,
            int deaths = 2, int kills = 200, double bars = 10, bool won = true) =>
            new RunStats(players, seconds, charged, chargeSeconds, deaths, kills, bars, won);

        // ---- RunScore ----

        [Fact]
        public void Compute_AllMetricsAtPar_TotalIsHalf()
        {
            // 240 s por teleportador, 1 morte por jogador, 0.5 vida/jogador/min, 20 abates/min
            var r = RunScore.Compute(Stats(), Pars);
            Assert.Equal(0.5f, r.Speed, 3);
            Assert.Equal(0.5f, r.Survival, 3);
            Assert.Equal(0.5f, r.Damage, 3);
            Assert.Equal(0.5f, r.Kills, 3);
            Assert.Equal(0.5f, r.Total, 3);
        }

        [Fact]
        public void Compute_PerfectRun_ApproachesOne()
        {
            var r = RunScore.Compute(Stats(chargeSeconds: 0, deaths: 0, bars: 0, kills: 1_000_000), Pars);
            Assert.True(r.Total > 0.99f);
        }

        [Fact]
        public void Compute_TerribleRun_ApproachesZero()
        {
            var r = RunScore.Compute(Stats(chargeSeconds: 1e9, deaths: 1_000_000, bars: 1e9, kills: 0), Pars);
            Assert.True(r.Total < 0.01f);
        }

        [Fact]
        public void Compute_NoTeleporterNoTime_NeutralForMissingData()
        {
            var r = RunScore.Compute(Stats(seconds: 0, charged: 0, chargeSeconds: 0), Pars);
            Assert.Equal(RunScore.Neutral, r.Speed);
            Assert.Equal(RunScore.Neutral, r.Damage);
            Assert.Equal(RunScore.Neutral, r.Kills);
        }

        [Fact]
        public void Compute_ZeroPlayers_TreatedAsOne() =>
            Assert.Equal(RunScore.Compute(Stats(players: 1), Pars).Total, RunScore.Compute(Stats(players: 0), Pars).Total);

        [Theory]
        [InlineData(0f)] [InlineData(-1f)] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)]
        public void Compute_InvalidPar_Neutral(float par)
        {
            var r = RunScore.Compute(Stats(), new ScorePars(par, par, par, par));
            Assert.Equal(RunScore.Neutral, r.Total);
        }

        // ---- ProgressionLadder.Next ----

        [Fact] public void Next_WinAboveThreshold_RaisesOne() => Assert.Equal(3, ProgressionLadder.Next(2, 0.7f, true, Rules));
        [Fact] public void Next_WinAtThreshold_Raises() => Assert.Equal(1, ProgressionLadder.Next(0, 0.5f, true, Rules));
        [Fact] public void Next_WinBelowThreshold_Holds() => Assert.Equal(2, ProgressionLadder.Next(2, 0.3f, true, Rules));
        [Fact] public void Next_Loss_LowersOne() => Assert.Equal(1, ProgressionLadder.Next(2, 0.9f, false, Rules));
        [Fact] public void Next_LossAtZero_StaysZero() => Assert.Equal(0, ProgressionLadder.Next(0, 0f, false, Rules));
        [Fact] public void Next_AtMaxLevel_DoesNotRaise() => Assert.Equal(4, ProgressionLadder.Next(4, 1f, true, Rules));
        [Fact] public void Next_NegativeLevel_TreatedAsZero() => Assert.Equal(1, ProgressionLadder.Next(-5, 1f, true, Rules));
        [Fact] public void Next_NaNScore_Holds() => Assert.Equal(2, ProgressionLadder.Next(2, float.NaN, true, Rules));
        [Fact] public void Next_LevelAboveNewMax_ClampedToMax() => Assert.Equal(4, ProgressionLadder.Next(50, 0.3f, true, Rules));

        // ---- Multiplier / MaxLevel ----

        [Fact] public void Multiplier_LevelZero_IsOne() => Assert.Equal(1f, ProgressionLadder.Multiplier(0, Rules));
        [Fact] public void Multiplier_ScalesByStep() => Assert.Equal(1.5f, ProgressionLadder.Multiplier(2, Rules));
        [Fact] public void Multiplier_SaturatesAtMax() => Assert.Equal(2f, ProgressionLadder.Multiplier(100, Rules));
        [Fact] public void MaxLevel_ExactDivision() => Assert.Equal(4, ProgressionLadder.MaxLevel(Rules));
        [Fact] public void MaxLevel_RoundsUpToSaturation() => Assert.Equal(14, ProgressionLadder.MaxLevel(new LadderRules(0.5f, 0.15f, 3f)));

        [Theory]
        [InlineData(0f, 2f)] [InlineData(-1f, 2f)] [InlineData(float.NaN, 2f)] [InlineData(0.25f, 1f)] [InlineData(0.25f, float.NaN)]
        public void InvalidRules_NoEffect(float step, float max)
        {
            var r = new LadderRules(0.5f, step, max);
            Assert.Equal(1f, ProgressionLadder.Multiplier(3, r));
            Assert.Equal(0, ProgressionLadder.Next(0, 1f, true, r));
        }

        // ---- Arquivo ----

        [Fact]
        public void SerializeParse_RoundTrip()
        {
            ProgressionLadder.Parse(ProgressionLadder.Serialize(7, 12), out var level, out var runs);
            Assert.Equal(7, level);
            Assert.Equal(12, runs);
        }

        [Theory]
        [InlineData(null)] [InlineData("")] [InlineData("lixo")] [InlineData("level=-3\nruns=abc")]
        public void Parse_Garbage_Zero(string text)
        {
            ProgressionLadder.Parse(text, out var level, out var runs);
            Assert.Equal(0, level);
            Assert.Equal(0, runs);
        }

        [Fact]
        public void Parse_WindowsLineEndingsAndSpaces()
        {
            ProgressionLadder.Parse(" level = 3 \r\nruns=5\r\n", out var level, out var runs);
            Assert.Equal(3, level);
            Assert.Equal(5, runs);
        }

        [Fact]
        public void Summary_MentionsLevelChange()
        {
            var s = Stats();
            var text = ProgressionLadder.Summary(s, RunScore.Compute(s, Pars), 1, 2, 1.5f);
            Assert.Contains("nível 1 -> 2", text);
            Assert.Contains("x1.50", text);
        }
    }
}
