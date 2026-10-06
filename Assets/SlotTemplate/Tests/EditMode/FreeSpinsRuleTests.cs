using System.Collections.Generic;
using NUnit.Framework;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Simulation;
using SlotTemplate.Core.Spin;
using SlotTemplate.Features.FreeSpins;
using static SlotTemplate.Tests.TestGame;

namespace SlotTemplate.Tests
{
    public class FreeSpinsRuleTests
    {
        private static FreeSpinsSettings Settings() => new FreeSpinsSettings
        {
            triggerSymbolId = "SC",
            spinsByCount = new[] { 0, 0, 5 },
            reelSetId = "free",
        };

        private static SpinEngine Engine(FreeSpinsRule rule, params int[] stops)
        {
            var rules = PayRules();
            rules.Add(rule);
            return new SpinEngine(Math(), new ScriptedRandom(stops), rules);
        }

        [Test]
        public void ThreeScatters_AwardFreeSpins()
        {
            var rule = new FreeSpinsRule(Math(), Settings());
            var engine = Engine(rule, 0);

            // Stop 2 puts SC (strip index 4) in the bottom row of every reel.
            var result = engine.Evaluate(new SpinRequest(1, 3), new[] { 2, 2, 2 });

            Assert.That(result.TryGet<FreeSpinsResult>(out var award), Is.True);
            Assert.That(award.SpinsAwarded, Is.EqualTo(5));
            Assert.That(award.TriggerPositions, Has.Count.EqualTo(3));
        }

        [Test]
        public void TwoScatters_DoNotTrigger()
        {
            var rule = new FreeSpinsRule(Math(), Settings());
            var result = Engine(rule, 0).Evaluate(new SpinRequest(1, 3), new[] { 2, 2, 0 });

            Assert.That(result.Has<FreeSpinsResult>(), Is.False);
        }

        [Test]
        public void Simulate_PlaysEveryAwardedSpinOnTheFreeReelSet()
        {
            var rule = new FreeSpinsRule(Math(), Settings());
            // Free strips are "CH,BE,SEV": stop 0 everywhere gives CH/BE/SEV rows, so every line pays.
            var engine = Engine(rule, 0);
            var trigger = engine.Evaluate(new SpinRequest(1, 3), new[] { 2, 2, 2 });
            long perSpin = engine.Evaluate(new SpinRequest(1, 3).ForFeature("free"), new[] { 0, 0, 0 }).TotalWin;

            long total = rule.Simulate(trigger, engine);

            Assert.That(perSpin, Is.GreaterThan(0));
            Assert.That(total, Is.EqualTo(perSpin * 5));
        }

        [Test]
        public void RtpSimulator_CountsFeatureTriggers()
        {
            var math = Math();
            var rule = new FreeSpinsRule(math, Settings());
            var rules = new List<ISpinRule>(PayRules()) { rule };

            var simulator = new RtpSimulator(math, rules, new SpinRequest(1, 3), seed: 11);
            simulator.Run(20000);

            Assert.That(simulator.Report.FeatureTriggers[FreeSpinsRule.Id], Is.GreaterThan(0));
            Assert.That(simulator.Report.FeatureWin, Is.GreaterThan(0));
        }
    }
}
