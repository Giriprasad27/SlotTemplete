using NUnit.Framework;
using SlotTemplate.Core.Evaluation;
using static SlotTemplate.Tests.TestRules;

namespace SlotTemplate.Tests
{
    public class PaylineWinEvaluatorTests
    {
        private PaylineWinEvaluator _evaluator;

        [SetUp]
        public void SetUp() => _evaluator = new PaylineWinEvaluator(Create());

        [Test]
        public void ThreeOfAKind_OnMiddleLine_PaysLineBetTimesMultiplier()
        {
            var grid = Grid(
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Bell, Bell },
                new[] { Cherry, Seven, Bell });

            var outcome = _evaluator.Evaluate(grid, lineBet: 2, totalBet: 6);

            Assert.That(outcome.LineWins, Has.Count.EqualTo(1));
            Assert.That(outcome.LineWins[0].PaylineIndex, Is.EqualTo(0));
            Assert.That(outcome.LineWins[0].SymbolId, Is.EqualTo(Bell));
            Assert.That(outcome.LineWins[0].Count, Is.EqualTo(3));
            Assert.That(outcome.TotalWin, Is.EqualTo(20));
        }

        [Test]
        public void Wild_SubstitutesForRegularSymbol()
        {
            var grid = Grid(
                new[] { Cherry, Bell, Bell },
                new[] { Seven, Wild, Seven },
                new[] { Bell, Cherry, Cherry });

            var outcome = _evaluator.Evaluate(grid, 1, 3);

            Assert.That(outcome.LineWins, Has.Count.EqualTo(1));
            Assert.That(outcome.LineWins[0].SymbolId, Is.EqualTo(Seven));
            Assert.That(outcome.LineWins[0].Payout, Is.EqualTo(50));
        }

        [Test]
        public void LeadingWilds_PayAsWild_WhenThatBeatsTheSymbolRun()
        {
            var grid = Grid(
                new[] { Wild, Wild, Wild },
                new[] { Cherry, Bell, Seven },
                new[] { Bell, Cherry, Bell });

            var outcome = _evaluator.Evaluate(grid, 1, 3);

            Assert.That(outcome.LineWins, Has.Count.EqualTo(1));
            Assert.That(outcome.LineWins[0].SymbolId, Is.EqualTo(Wild));
            Assert.That(outcome.LineWins[0].Payout, Is.EqualTo(100));
        }

        [Test]
        public void PartialRun_PaysWhenCountHasAPayout()
        {
            var grid = Grid(
                new[] { Seven, Seven, Bell },
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Cherry, Bell });

            var outcome = _evaluator.Evaluate(grid, 1, 3);

            Assert.That(outcome.LineWins, Has.Count.EqualTo(1));
            Assert.That(outcome.LineWins[0].Count, Is.EqualTo(2));
            Assert.That(outcome.LineWins[0].Positions, Has.Count.EqualTo(2));
            Assert.That(outcome.TotalWin, Is.EqualTo(2));
        }

        [Test]
        public void BrokenRun_DoesNotPay()
        {
            var grid = Grid(
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Cherry, Bell },
                new[] { Seven, Bell, Seven });

            var outcome = _evaluator.Evaluate(grid, 1, 3);

            Assert.That(outcome.IsWin, Is.False);
        }

        [Test]
        public void Scatter_PaysTotalBet_AnywhereOnGrid()
        {
            var grid = Grid(
                new[] { Scatter, Bell, Cherry },
                new[] { Bell, Cherry, Bell },
                new[] { Cherry, Seven, Scatter });

            var outcome = _evaluator.Evaluate(grid, lineBet: 1, totalBet: 3);

            Assert.That(outcome.LineWins, Is.Empty);
            Assert.That(outcome.ScatterWins, Has.Count.EqualTo(1));
            Assert.That(outcome.ScatterWins[0].Count, Is.EqualTo(2));
            Assert.That(outcome.TotalWin, Is.EqualTo(3));
        }

        [Test]
        public void Scatter_BreaksAPaylineRun()
        {
            var grid = Grid(
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Scatter, Bell },
                new[] { Seven, Bell, Seven });

            var outcome = _evaluator.Evaluate(grid, 1, 3);

            Assert.That(outcome.LineWins, Is.Empty);
        }
    }
}
