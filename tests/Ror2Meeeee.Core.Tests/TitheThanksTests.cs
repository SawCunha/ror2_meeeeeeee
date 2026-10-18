using Ror2Meeeee.Core.Gold;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class TitheThanksTests
    {
        [Fact] public void Due_IntervalElapsedWithCredit_True() => Assert.True(TitheThanks.Due(100, 40, 60, 5));
        [Fact] public void Due_IntervalNotElapsed_False() => Assert.False(TitheThanks.Due(99, 40, 60, 5));
        [Fact] public void Due_NothingPending_False() => Assert.False(TitheThanks.Due(1000, 0, 60, 0.5));
        [Fact] public void Due_IntervalZero_Off() => Assert.False(TitheThanks.Due(1000, 0, 0, 500));
        [Fact] public void Due_NegativeInterval_Off() => Assert.False(TitheThanks.Due(1000, 0, -1, 500));

        [Fact]
        public void Format_ShowsCreditAndGold()
        {
            var text = TitheThanks.Format(0, 1234.9, 12345);
            Assert.Contains("1.234", text);      // crédito truncado, milhar brasileiro
            Assert.Contains("R$ 12.345", text);
            Assert.StartsWith("<color=#c792ea>Diretor:</color> ", text);
        }

        [Fact]
        public void Format_English_UsesEnglishThousands()
        {
            var text = TitheThanks.Format(0, 1234.9, 12345, english: true);
            Assert.Contains("1,234", text);
            Assert.Contains("$12,345", text);
            Assert.StartsWith(TitheThanks.PrefixEn, text);
        }

        [Fact] public void Lines_SameCountInBothLanguages() => Assert.Equal(TitheThanks.LinesPt.Length, TitheThanks.LinesEn.Length);

        [Fact] public void Format_RotatesLines() => Assert.NotEqual(TitheThanks.Format(0, 10, 100), TitheThanks.Format(1, 10, 100));
        [Fact] public void Format_IndexWraps() => Assert.Equal(TitheThanks.Format(0, 10, 100), TitheThanks.Format(4, 10, 100));
        [Fact] public void Format_NegativeIndex_DoesNotThrow() => Assert.NotNull(TitheThanks.Format(-1, 10, 100));
    }
}
