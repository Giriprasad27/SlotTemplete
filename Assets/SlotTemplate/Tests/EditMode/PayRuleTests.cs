using NUnit.Framework;
using SlotTemplate.Core.Spin;
using static SlotTemplate.Tests.TestGame;

namespace SlotTemplate.Tests
{
    public class PayRuleTests
    {
        [Test]
        public void ThreeOfAKind_OnMiddleLine_PaysLineBetTimesMultiplier()
        {
            var result = Evaluate(
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Bell, Bell },
                new[] { Cherry, Seven, Bell }, lineBet: 2);

            Assert.That(result.Wins, Has.Count.EqualTo(1));
            Assert.That(result.Wins[0].PaylineIndex, Is.EqualTo(0));
            Assert.That(result.Wins[0].SymbolIndex, Is.EqualTo(Bell));
            Assert.That(result.TotalWin, Is.EqualTo(20));
        }

        [Test]
        public void Wild_SubstitutesForRegularSymbol()
        {
            var result = Evaluate(
                new[] { Cherry, Bell, Bell },
                new[] { Seven, Wild, Seven },
                new[] { Bell, Cherry, Cherry });

            Assert.That(result.Wins, Has.Count.EqualTo(1));
            Assert.That(result.Wins[0].SymbolIndex, Is.EqualTo(Seven));
            Assert.That(result.TotalWin, Is.EqualTo(50));
        }

        [Test]
        public void LeadingWilds_PayAsWild_WhenThatIsWorthMore()
        {
            var result = Evaluate(
                new[] { Wild, Wild, Wild },
                new[] { Cherry, Bell, Seven },
                new[] { Bell, Cherry, Bell });

            Assert.That(result.Wins[0].SymbolIndex, Is.EqualTo(Wild));
            Assert.That(result.TotalWin, Is.EqualTo(100));
        }

        [Test]
        public void PartialRun_PaysWhenThatCountHasAPayout()
        {
            var result = Evaluate(
                new[] { Seven, Seven, Bell },
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Cherry, Bell });

            Assert.That(result.Wins[0].Count, Is.EqualTo(2));
            Assert.That(result.TotalWin, Is.EqualTo(2));
        }

        [Test]
        public void BrokenRun_DoesNotPay()
        {
            var result = Evaluate(
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Cherry, Bell },
                new[] { Seven, Bell, Seven });

            Assert.That(result.IsWin, Is.False);
        }

        [Test]
        public void Scatter_PaysTotalBet_AnywhereOnGrid()
        {
            var result = Evaluate(
                new[] { Scatter, Bell, Cherry },
                new[] { Bell, Cherry, Bell },
                new[] { Cherry, Seven, Scatter });

            Assert.That(result.Wins, Has.Count.EqualTo(1));
            Assert.That(result.Wins[0].Kind, Is.EqualTo(WinKind.Scatter));
            Assert.That(result.TotalWin, Is.EqualTo(3)); // 1x total bet of 3
        }

        [Test]
        public void Scatter_BreaksAPaylineRun()
        {
            var result = Evaluate(
                new[] { Cherry, Bell, Cherry },
                new[] { Bell, Scatter, Bell },
                new[] { Seven, Bell, Seven });

            Assert.That(result.Wins, Is.Empty);
        }

        [Test]
        public void TypedResultSlots_StoreOneValuePerType()
        {
            var result = Evaluate(new[] { Cherry, Bell, Cherry }, new[] { Bell, Cherry, Bell }, new[] { Seven, Bell, Seven });
            var marker = new object[0];

            Assert.That(result.TryGet<object[]>(out _), Is.False);
            result.Set(marker);
            Assert.That(result.Get<object[]>(), Is.SameAs(marker));
        }
    }
}
