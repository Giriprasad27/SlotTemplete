using NUnit.Framework;
using SlotTemplate.Core;
using SlotTemplate.Core.Economy;
using SlotTemplate.Core.Random;
using static SlotTemplate.Tests.TestRules;

namespace SlotTemplate.Tests
{
    public class SlotMachineTests
    {
        private static SlotMachine CreateMachine(long balance, params int[] stops)
        {
            var rules = Create();
            return new SlotMachine(
                rules,
                new Wallet(balance),
                new BetModel(new long[] { 1, 2, 5 }, rules.Paylines.Count),
                new ScriptedRandomNumberGenerator(stops));
        }

        [Test]
        public void TrySpin_DebitsTotalBet_AndDefersWinUntilCollected()
        {
            // Stop 0 on every reel puts Bell (strip index 1) on the middle line.
            var machine = CreateMachine(100, 0, 0, 0);

            Assert.That(machine.TrySpin(out var outcome), Is.True);
            Assert.That(machine.Wallet.Balance, Is.EqualTo(97));
            Assert.That(outcome.TotalWin, Is.GreaterThan(0));

            machine.CollectWin();

            Assert.That(machine.Wallet.Balance, Is.EqualTo(97 + outcome.TotalWin));
        }

        [Test]
        public void TrySpin_Fails_WhenBalanceIsTooLow()
        {
            var machine = CreateMachine(2, 0, 0, 0);

            Assert.That(machine.CanSpin, Is.False);
            Assert.That(machine.TrySpin(out var outcome), Is.False);
            Assert.That(outcome, Is.Null);
            Assert.That(machine.Wallet.Balance, Is.EqualTo(2));
        }

        [Test]
        public void CollectWin_IsIdempotent()
        {
            var machine = CreateMachine(100, 0, 0, 0);
            machine.TrySpin(out var outcome);

            machine.CollectWin();
            machine.CollectWin();

            Assert.That(machine.Wallet.Balance, Is.EqualTo(97 + outcome.TotalWin));
        }
    }
}
