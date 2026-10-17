using Ror2Meeeee.Core.Spend;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    public class SpendLedgerTests
    {
        [Fact]
        public void Accumulates_PerPlayer()
        {
            var l = new SpendLedger();
            l.Add(1, () => "A", 25);
            l.Add(1, () => "A", 50);
            l.Add(2, () => "B", 10);
            var rows = l.Snapshot();
            Assert.Equal(2, rows.Length);
            Assert.Equal("A", rows[0].Name);
            Assert.Equal(75, rows[0].Gold);
            Assert.Equal(2, rows[0].Purchases);
            Assert.Equal(10, rows[1].Gold);
        }

        [Fact]
        public void NonGoldPurchase_CountsButAddsNoGold()
        {
            var l = new SpendLedger();
            l.Add(1, () => "A", 0);
            var r = l.Snapshot()[0];
            Assert.Equal(1, r.Purchases);
            Assert.Equal(0, r.Gold);
        }

        [Fact]
        public void NameResolvedOnlyOnce()
        {
            var l = new SpendLedger();
            var calls = 0;
            l.Add(1, () => { calls++; return "A"; }, 1);
            l.Add(1, () => { calls++; return "A"; }, 1);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Gold_SaturatesWithoutOverflow()
        {
            var l = new SpendLedger();
            l.Add(1, () => "A", long.MaxValue);
            l.Add(1, () => "A", 100);
            Assert.Equal(long.MaxValue, l.Snapshot()[0].Gold);
        }

        [Fact]
        public void Clear_Empties_AndFormatSaysSo()
        {
            var l = new SpendLedger();
            l.Add(1, () => "A", 5);
            l.Clear();
            Assert.Equal(0, l.Players);
            Assert.Contains("Nenhuma", SpendLedger.Format(l.Snapshot()));
        }

        [Fact]
        public void Format_ListsRowsAndTotal()
        {
            var l = new SpendLedger();
            l.Add(1, () => "A", 30);
            l.Add(2, () => "B", 20);
            var text = SpendLedger.Format(l.Snapshot());
            Assert.Contains("A | 1 | 30", text);
            Assert.Contains("Total | | 50", text);
        }
    }
}
