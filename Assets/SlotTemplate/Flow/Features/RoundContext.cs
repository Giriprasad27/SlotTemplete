using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Presentation;
using SlotTemplate.Flow.Round;

namespace SlotTemplate.Flow.Features
{
    /// <summary>What a feature gets while it plays: the trigger, a way to spin, and a way to pay.</summary>
    public sealed class RoundContext
    {
        private readonly RoundRunner _runner;

        public string FeatureId { get; }

        /// <summary>The spin that triggered the feature.</summary>
        public SpinResult Trigger { get; }

        /// <summary>The bet the round was paid with. Feature spins reuse it via <see cref="SpinRequest.ForFeature"/>.</summary>
        public SpinRequest BaseRequest => Trigger.Request;

        /// <summary>Progress saved by an earlier, interrupted run of this feature; null on a fresh start.</summary>
        public string SavedState { get; }

        public SkipSignal Skip { get; }

        /// <summary>Total paid so far this round, base game included.</summary>
        public long RoundWin => _runner.CurrentRoundWin;

        internal RoundContext(RoundRunner runner, string featureId, SpinResult trigger, string savedState, SkipSignal skip)
        {
            _runner = runner;
            FeatureId = featureId;
            Trigger = trigger;
            SavedState = savedState;
            Skip = skip;
        }

        /// <summary>
        /// Gets a result, spins the reels to it and shows its wins, without taking a bet or paying.
        /// Pay the win yourself with <see cref="Pay"/>.
        /// </summary>
        public UniTask<SpinResult> SpinAsync(SpinRequest request, CancellationToken ct) => _runner.PlayFeatureSpin(request, ct);

        /// <summary>Credits <paramref name="amount"/> and saves <paramref name="state"/> in one write.</summary>
        public void Pay(long amount, string state) => _runner.PayFeature(FeatureId, amount, state);

        public void SaveState(string state) => _runner.PayFeature(FeatureId, 0, state);
    }
}
