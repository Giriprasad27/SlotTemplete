using System;
using System.Collections.Generic;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core.Spin
{
    /// <summary>Picks reel stops, builds the grid and runs every <see cref="ISpinRule"/> on it.</summary>
    public sealed class SpinEngine
    {
        private readonly IRandomNumberGenerator _rng;
        private readonly List<ISpinRule> _rules;

        public SlotMath Math { get; }
        public IReadOnlyList<ISpinRule> Rules => _rules;

        public SpinEngine(SlotMath math, IRandomNumberGenerator rng, IEnumerable<ISpinRule> rules)
        {
            Math = math ?? throw new ArgumentNullException(nameof(math));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _rules = new List<ISpinRule>(rules ?? throw new ArgumentNullException(nameof(rules)));
        }

        public SpinResult Spin(SpinRequest request)
        {
            var set = Math.GetReelSet(request.ReelSetId);
            var stops = new int[set.ReelCount];
            for (int reel = 0; reel < stops.Length; reel++)
                stops[reel] = _rng.Next(set.Reels[reel].Length);

            return Evaluate(request, stops);
        }

        /// <summary>Evaluates known stops. Used for crash recovery, replays and forced outcomes.</summary>
        public SpinResult Evaluate(SpinRequest request, IReadOnlyList<int> stops)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (stops == null) throw new ArgumentNullException(nameof(stops));

            var set = Math.GetReelSet(request.ReelSetId);
            if (stops.Count != set.ReelCount)
                throw new ArgumentException($"Expected {set.ReelCount} stops, got {stops.Count}.", nameof(stops));

            var symbols = new int[set.ReelCount, Math.RowCount];
            var normalized = new int[set.ReelCount];
            for (int reel = 0; reel < set.ReelCount; reel++)
            {
                var strip = set.Reels[reel];
                normalized[reel] = strip.Wrap(stops[reel]);
                for (int row = 0; row < Math.RowCount; row++)
                    symbols[reel, row] = strip.GetSymbol(normalized[reel] + row);
            }

            var grid = new Grid(set.Id, symbols, normalized);
            var result = new SpinResult(request, grid);
            var context = new SpinContext(Math, request, grid, result);
            foreach (var rule in _rules) rule.Apply(context);
            return result;
        }
    }
}
