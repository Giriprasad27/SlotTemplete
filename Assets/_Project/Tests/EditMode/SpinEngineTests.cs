using NUnit.Framework;
using SlotTemplate.Core;
using SlotTemplate.Core.Random;
using static SlotTemplate.Tests.TestRules;

namespace SlotTemplate.Tests
{
    public class SpinEngineTests
    {
        [Test]
        public void Spin_ShowsConsecutiveStripSymbols_FromEachStop()
        {
            var engine = new SpinEngine(Create(), new ScriptedRandomNumberGenerator(0, 1, 2));

            var grid = engine.Spin();

            Assert.That(grid.Stops, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(grid[0, 0], Is.EqualTo(Cherry));
            Assert.That(grid[0, 2], Is.EqualTo(Seven));
            Assert.That(grid[2, 0], Is.EqualTo(Seven));
        }

        [Test]
        public void BuildGrid_WrapsAroundTheEndOfTheStrip()
        {
            var engine = new SpinEngine(Create(), new ScriptedRandomNumberGenerator(0));

            var grid = engine.BuildGrid(new[] { 4, 4, -1 });

            Assert.That(grid[0, 0], Is.EqualTo(Scatter));
            Assert.That(grid[0, 1], Is.EqualTo(Cherry));
            Assert.That(grid[0, 2], Is.EqualTo(Bell));
            Assert.That(grid.Stops[2], Is.EqualTo(4));
        }
    }
}
