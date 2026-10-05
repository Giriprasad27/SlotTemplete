using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Spin;
using SlotTemplate.Features.FreeSpins;
using SlotTemplate.Flow.Economy;
using SlotTemplate.Flow.Features;
using SlotTemplate.Flow.Round;
using SlotTemplate.Flow.Save;
using SlotTemplate.Flow.State;
using static SlotTemplate.Tests.TestGame;

namespace SlotTemplate.Tests
{
    public class RoundRunnerTests
    {
        // Strip "CH,BE,SEV,WILD,SC". Stop 0 on all reels: CH / BE / SEV rows, every line pays.
        private static readonly int[] AllLinesWin = { 0, 0, 0 };
        // Stop 2 on all reels: SEV / WILD / SC rows -> 3 scatters (free spins) plus wins.
        private static readonly int[] ScatterTrigger = { 2, 2, 2 };
        // Mixed rows with no line, no scatter pair.
        private static readonly int[] NoWin = { 0, 2, 1 };

        private sealed class Rig
        {
            public InMemorySaveStore Store;
            public SlotGame Game;
            public RoundJournal Journal;
            public BetModel Bet;
            public SpinEngine Engine;
            public ScriptedOutcome Outcome;
            public FakeReels Reels;
            public FakeWins Wins;
            public RoundRunner Runner;
        }

        private static Rig Create(InMemorySaveStore store = null, long balance = 100, bool withFreeSpins = false)
        {
            var math = Math();
            var rules = PayRules();
            var features = new List<ISlotFeature>();
            if (withFreeSpins)
            {
                var settings = new FreeSpinsSettings { triggerSymbolId = "SC", spinsByCount = new[] { 0, 0, 3 }, reelSetId = "free" };
                rules.Add(new FreeSpinsRule(math, settings));
                features.Add(new FreeSpinsFeature(settings, null));
            }

            var rig = new Rig { Store = store ?? new InMemorySaveStore() };
            rig.Engine = new SpinEngine(math, new ScriptedRandom(0), rules);
            rig.Outcome = new ScriptedOutcome(rig.Engine);
            rig.Game = new SlotGame();
            rig.Journal = new RoundJournal(rig.Store, balance);
            rig.Bet = new BetModel(new long[] { 1, 2 }, math.Paylines.Count);
            rig.Reels = new FakeReels();
            rig.Wins = new FakeWins();
            rig.Runner = new RoundRunner(rig.Game, rig.Outcome, rig.Journal, rig.Bet, rig.Reels, rig.Wins, features);
            rig.Runner.BootAsync(CancellationToken.None).Wait();
            return rig;
        }

        private static long WinFor(Rig rig, int[] stops) => rig.Engine.Evaluate(new SpinRequest(1, 3), stops).TotalWin;

        [Test]
        public void PlayRound_TakesBet_PaysWin_AndReturnsToIdle()
        {
            var rig = Create();
            rig.Outcome.Queue(AllLinesWin);
            long expectedWin = WinFor(rig, AllLinesWin);

            var summary = rig.Runner.PlayRound(CancellationToken.None).Result();

            Assert.That(summary.Played, Is.True);
            Assert.That(summary.TotalWin, Is.EqualTo(expectedWin));
            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(100 - 3 + expectedWin));
            Assert.That(rig.Game.Current, Is.EqualTo(GameState.Idle));
            Assert.That(rig.Journal.HasPendingRound, Is.False);
            Assert.That(rig.Store.Load().balance, Is.EqualTo(rig.Journal.Wallet.Balance));
        }

        [Test]
        public void PlayRound_WithoutEnoughCredits_ChangesNothing()
        {
            var rig = Create(balance: 2);
            string notice = null;
            rig.Runner.Notice += message => notice = message;

            var summary = rig.Runner.PlayRound(CancellationToken.None).Result();

            Assert.That(summary.Played, Is.False);
            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(2));
            Assert.That(rig.Outcome.SpinCalls, Is.Zero);
            Assert.That(notice, Is.Not.Null);
            Assert.That(rig.Game.Current, Is.EqualTo(GameState.Idle));
        }

        [Test]
        public void OutcomeFailure_RefundsBet_AndUnlocksInput()
        {
            var rig = Create();
            rig.Outcome.FailNext = new TimeoutException("server timeout");

            Assert.Throws<TimeoutException>(() => rig.Runner.PlayRound(CancellationToken.None).Result());

            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(100));
            Assert.That(rig.Journal.HasPendingRound, Is.False);
            Assert.That(rig.Game.Current, Is.EqualTo(GameState.Idle));
        }

        [Test]
        public void PresenterFailure_KeepsRoundSaved_AndNextSpinFinishesIt()
        {
            var rig = Create();
            rig.Outcome.Queue(AllLinesWin);
            rig.Reels.ThrowOnSpin = new InvalidOperationException("animation broke");
            long expectedWin = WinFor(rig, AllLinesWin);

            Assert.Throws<InvalidOperationException>(() => rig.Runner.PlayRound(CancellationToken.None).Result());
            Assert.That(rig.Journal.HasPendingRound, Is.True);
            Assert.That(rig.Game.Current, Is.EqualTo(GameState.Idle));
            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(97));

            var summary = rig.Runner.PlayRound(CancellationToken.None).Result();

            Assert.That(summary.Resumed, Is.True);
            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(97 + expectedWin));
            Assert.That(rig.Outcome.SpinCalls, Is.EqualTo(1), "the saved result is replayed, not re-rolled");
        }

        [Test]
        public void CrashAfterBet_BeforeResult_IsRefundedOnBoot()
        {
            var store = new InMemorySaveStore();
            var crashed = new RoundJournal(store, 100);
            crashed.BeginRound(new SpinRequest(1, 3));

            var rig = Create(store);

            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(100));
            Assert.That(rig.Journal.HasPendingRound, Is.False);
            Assert.That(rig.Game.Current, Is.EqualTo(GameState.Idle));
        }

        [Test]
        public void CrashAfterResult_IsReplayedAndPaidOnceOnBoot()
        {
            var store = new InMemorySaveStore();
            var first = Create(store);
            long expectedWin = WinFor(first, AllLinesWin);
            first.Journal.BeginRound(new SpinRequest(1, 3));
            first.Journal.RecordResult(AllLinesWin);
            // "Crash": throw away everything but the store.

            var rig = Create(store);

            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(97 + expectedWin));
            Assert.That(rig.Journal.HasPendingRound, Is.False);
            Assert.That(rig.Reels.SpinCount, Is.EqualTo(1));
        }

        [Test]
        public void CrashAfterPayment_DoesNotPayTwice()
        {
            var store = new InMemorySaveStore();
            var first = Create(store);
            long expectedWin = WinFor(first, AllLinesWin);
            first.Journal.BeginRound(new SpinRequest(1, 3));
            first.Journal.RecordResult(AllLinesWin);
            first.Journal.PayBase(expectedWin);

            var rig = Create(store);

            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(97 + expectedWin));
            Assert.That(rig.Reels.SpinCount, Is.Zero, "already-paid results are shown, not spun again");
            Assert.That(rig.Reels.ShowCount, Is.EqualTo(1));
        }

        [Test]
        public void FreeSpins_PlayAllAwardedSpins_InFeatureState_WithoutTakingBets()
        {
            var rig = Create(withFreeSpins: true);
            rig.Outcome.Queue(ScatterTrigger);
            var states = new List<GameState>();
            rig.Game.StateChanged += (from, to) => states.Add(to);
            long baseWin = WinFor(rig, ScatterTrigger);
            long freeWin = rig.Engine.Evaluate(new SpinRequest(1, 3).ForFeature("free"), new[] { 0, 0, 0 }).TotalWin;

            var summary = rig.Runner.PlayRound(CancellationToken.None).Result();

            Assert.That(summary.FeatureTriggered, Is.True);
            Assert.That(rig.Outcome.SpinCalls, Is.EqualTo(1 + 3));
            Assert.That(summary.TotalWin, Is.EqualTo(baseWin + 3 * freeWin));
            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(100 - 3 + baseWin + 3 * freeWin));
            Assert.That(states, Is.EqualTo(new[] { GameState.InRound, GameState.InFeature, GameState.Idle }));
        }

        [Test]
        public void CrashDuringFreeSpins_ResumesWithRemainingSpins()
        {
            var store = new InMemorySaveStore();
            var first = Create(store, withFreeSpins: true);
            long baseWin = WinFor(first, ScatterTrigger);
            first.Journal.BeginRound(new SpinRequest(1, 3));
            first.Journal.RecordResult(ScatterTrigger);
            first.Journal.PayBase(baseWin);
            first.Journal.PayFeature(FreeSpinsRule.Id, 7, "1|2|7"); // 2 played, 1 left, 7 won so far

            var rig = Create(store, withFreeSpins: true);
            long freeWin = rig.Engine.Evaluate(new SpinRequest(1, 3).ForFeature("free"), new[] { 0, 0, 0 }).TotalWin;

            Assert.That(rig.Outcome.SpinCalls, Is.EqualTo(1), "only the one remaining free spin is played");
            Assert.That(rig.Journal.Wallet.Balance, Is.EqualTo(97 + baseWin + 7 + freeWin));
            Assert.That(rig.Journal.HasPendingRound, Is.False);
        }

        [Test]
        public void SkipSignal_DoesNotAbortTheRound()
        {
            var rig = Create();
            rig.Outcome.Queue(AllLinesWin);
            rig.Runner.Skip.Request();

            var summary = rig.Runner.PlayRound(CancellationToken.None).Result();

            Assert.That(summary.Played, Is.True);
            Assert.That(rig.Journal.HasPendingRound, Is.False);
        }

        [Test]
        public void StateMachine_RejectsIllegalTransitions()
        {
            var game = new SlotGame();
            Assert.Throws<InvalidOperationException>(() => game.Enter(GameState.InFeature));

            game.Enter(GameState.Idle);
            Assert.Throws<InvalidOperationException>(() => game.Enter(GameState.InFeature));
            Assert.Throws<InvalidOperationException>(() => game.Enter(GameState.Boot));
        }

        [Test]
        public void Autoplay_StopsWhenAFeatureTriggers()
        {
            var rig = Create(withFreeSpins: true);
            rig.Outcome.Queue(NoWin);
            rig.Outcome.Queue(ScatterTrigger);
            var autoplay = new AutoplayController(rig.Runner, rig.Journal.Wallet);

            autoplay.Run(new AutoplaySettings { spins = 10, stopOnFeature = true, stopOnWinMultiplier = 0 }, CancellationToken.None).Wait();

            Assert.That(rig.Outcome.SpinCalls, Is.EqualTo(2 + 3)); // two paid spins + three free spins
            Assert.That(autoplay.IsRunning, Is.False);
        }
    }
}
