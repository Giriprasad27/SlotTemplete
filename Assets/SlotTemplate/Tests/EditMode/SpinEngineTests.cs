using System.Collections.Generic;
using NUnit.Framework;
using SlotTemplate.Core.Definition;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Simulation;
using SlotTemplate.Core.Spin;
using static SlotTemplate.Tests.TestGame;

namespace SlotTemplate.Tests
{
    public class SpinEngineTests
    {
        private static SpinRequest Request => new SpinRequest(1, 3);

        [Test]
        public void Spin_ShowsConsecutiveStripSymbols_FromEachStop()
        {
            var engine = new SpinEngine(Math(), new ScriptedRandom(0, 1, 2), PayRules());

            var grid = engine.Spin(Request).Grid;

            Assert.That(grid.Stops, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(grid[0, 0], Is.EqualTo(Cherry));
            Assert.That(grid[0, 2], Is.EqualTo(Seven));
            Assert.That(grid[2, 0], Is.EqualTo(Seven));
        }

        [Test]
        public void Evaluate_WrapsAroundTheEndOfTheStrip()
        {
            var engine = new SpinEngine(Math(), new ScriptedRandom(0), PayRules());

            var grid = engine.Evaluate(Request, new[] { 4, 4, -1 }).Grid;

            Assert.That(grid[0, 0], Is.EqualTo(Scatter));
            Assert.That(grid[0, 1], Is.EqualTo(Cherry));
            Assert.That(grid.Stops[2], Is.EqualTo(4));
        }

        [Test]
        public void Evaluate_ReplaysTheSameResult_FromSavedStops()
        {
            var engine = new SpinEngine(Math(), new SeededRandom(7), PayRules());
            var original = engine.Spin(Request);

            var replay = engine.Evaluate(Request, original.Grid.Stops);

            Assert.That(replay.TotalWin, Is.EqualTo(original.TotalWin));
            Assert.That(replay.Wins.Count, Is.EqualTo(original.Wins.Count));
        }

        [Test]
        public void SeededRandom_IsDeterministic_AndStaysInRange()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 1000; i++)
            {
                int value = a.Next(7);
                Assert.That(value, Is.InRange(0, 6));
                Assert.That(b.Next(7), Is.EqualTo(value));
            }
        }

        [Test]
        public void Factory_ReportsUnknownSymbolsAndBadPaylines()
        {
            var data = Data();
            data.reelSets[0].reels[1] = "CH,NOPE";
            data.paylines.Add(new PaylineData(0, 3, 0));

            var errors = SlotMathFactory.Validate(data);

            Assert.That(errors, Has.Some.Contains("NOPE"));
            Assert.That(errors, Has.Some.Contains("row 3"));
        }

        [Test]
        public void RtpSimulator_MatchesExactRtp_ForASmallMachine()
        {
            // With 5 stops per reel there are 125 outcomes; enumerate them for the exact RTP.
            var math = Math();
            var engine = new SpinEngine(math, new ScriptedRandom(0), PayRules());
            long exactWin = 0;
            for (int a = 0; a < 5; a++)
            for (int b = 0; b < 5; b++)
            for (int c = 0; c < 5; c++)
                exactWin += engine.Evaluate(Request, new[] { a, b, c }).TotalWin;
            double exactRtp = exactWin / (125.0 * Request.TotalBet);

            var simulator = new RtpSimulator(math, PayRules(), Request, seed: 3);
            simulator.Run(200000);

            Assert.That(simulator.Report.Rtp, Is.EqualTo(exactRtp).Within(0.02));
            Assert.That(simulator.Report.Spins, Is.EqualTo(200000));
        }
    }
}
