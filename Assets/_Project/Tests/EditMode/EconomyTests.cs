using NUnit.Framework;
using SlotTemplate.Core.Economy;

namespace SlotTemplate.Tests
{
    public class EconomyTests
    {
        [Test]
        public void Wallet_RaisesBalanceChanged_OnDebitAndCredit()
        {
            var wallet = new Wallet(10);
            long last = -1;
            wallet.BalanceChanged += value => last = value;

            Assert.That(wallet.TryDebit(4), Is.True);
            Assert.That(last, Is.EqualTo(6));

            wallet.Credit(10);
            Assert.That(last, Is.EqualTo(16));
        }

        [Test]
        public void Wallet_RejectsOverdraft()
        {
            var wallet = new Wallet(3);

            Assert.That(wallet.TryDebit(4), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(3));
        }

        [Test]
        public void BetModel_TotalBet_IsLineBetTimesLines_AndClampsLevels()
        {
            var bet = new BetModel(new long[] { 1, 5, 10 }, lineCount: 20);

            Assert.That(bet.TotalBet, Is.EqualTo(20));

            bet.Decrease();
            Assert.That(bet.LevelIndex, Is.EqualTo(0));

            bet.Increase();
            bet.Increase();
            bet.Increase();
            Assert.That(bet.LineBet, Is.EqualTo(10));
            Assert.That(bet.TotalBet, Is.EqualTo(200));
            Assert.That(bet.CanIncrease, Is.False);
        }
    }
}
