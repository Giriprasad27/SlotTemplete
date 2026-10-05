using System;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core
{
    /// <summary>Picks a random stop on every reel and builds the visible grid.</summary>
    public sealed class SpinEngine
    {
        private readonly SlotRules _rules;
        private readonly IRandomNumberGenerator _rng;

        public SpinEngine(SlotRules rules, IRandomNumberGenerator rng)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public SpinGrid Spin()
        {
            var stops = new int[_rules.ReelCount];
            for (int reel = 0; reel < stops.Length; reel++)
                stops[reel] = _rng.Next(_rules.Reels[reel].Length);

            return BuildGrid(stops);
        }

        /// <summary>Builds the grid for known stops. Useful for replays and forced outcomes.</summary>
        public SpinGrid BuildGrid(int[] stops)
        {
            if (stops == null) throw new ArgumentNullException(nameof(stops));
            if (stops.Length != _rules.ReelCount)
                throw new ArgumentException("One stop index is required per reel.", nameof(stops));

            var symbols = new int[_rules.ReelCount, _rules.RowCount];
            var normalizedStops = new int[stops.Length];

            for (int reel = 0; reel < _rules.ReelCount; reel++)
            {
                var strip = _rules.Reels[reel];
                normalizedStops[reel] = strip.Wrap(stops[reel]);

                for (int row = 0; row < _rules.RowCount; row++)
                    symbols[reel, row] = strip.GetSymbol(normalizedStops[reel] + row);
            }

            return new SpinGrid(symbols, normalizedStops);
        }
    }
}
