using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Outcome;

namespace SlotTemplate.DebugTools
{
    /// <summary>
    /// Development-only decorator: returns queued forced outcomes, otherwise defers to the real provider.
    /// The assembly is compiled only in the editor and development builds.
    /// </summary>
    public sealed class ForcedOutcomeProvider : IOutcomeProvider
    {
        private readonly IOutcomeProvider _inner;
        private readonly SpinEngine _engine;
        private readonly Queue<int[]> _forcedStops = new Queue<int[]>();
        private readonly SeededRandom _searchRandom = new SeededRandom();

        public int QueuedCount => _forcedStops.Count;

        public ForcedOutcomeProvider(IOutcomeProvider inner, SpinEngine engine)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public UniTask<SpinResult> Spin(SpinRequest request, CancellationToken ct)
        {
            if (_forcedStops.Count == 0 || request.IsFeatureSpin) return _inner.Spin(request, ct);
            return UniTask.FromResult(_engine.Evaluate(request, _forcedStops.Dequeue()));
        }

        public UniTask<SpinResult> Restore(SpinRequest request, IReadOnlyList<int> stops, CancellationToken ct) =>
            _inner.Restore(request, stops, ct);

        /// <summary>Forces the next paid spin to land on exact stops.</summary>
        public void ForceStops(int[] stops) => _forcedStops.Enqueue((int[])stops.Clone());

        /// <summary>
        /// Searches random stops until one satisfies <paramref name="predicate"/>, then forces it.
        /// Returns false if nothing matched within <paramref name="maxTries"/>.
        /// </summary>
        public bool ForceWhere(SpinRequest request, Func<SpinResult, bool> predicate, int maxTries = 200000)
        {
            var set = _engine.Math.GetReelSet(request.ReelSetId);
            var stops = new int[set.ReelCount];

            for (int attempt = 0; attempt < maxTries; attempt++)
            {
                for (int reel = 0; reel < stops.Length; reel++) stops[reel] = _searchRandom.Next(set.Reels[reel].Length);
                if (predicate(_engine.Evaluate(request, stops)))
                {
                    ForceStops(stops);
                    return true;
                }
            }
            return false;
        }
    }
}
