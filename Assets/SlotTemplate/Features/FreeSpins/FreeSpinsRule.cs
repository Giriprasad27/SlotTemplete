using System;
using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Simulation;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Features.FreeSpins
{
    /// <summary>Math half of free spins: detects the trigger and plays the feature out for the RTP simulator.</summary>
    public sealed class FreeSpinsRule : ISpinRule, IFeatureSimulation
    {
        public const string Id = "FreeSpins";

        private readonly int _triggerSymbol;

        public FreeSpinsSettings Settings { get; }
        public string FeatureId => Id;

        public FreeSpinsRule(SlotMath math, FreeSpinsSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _triggerSymbol = math.Paytable.IndexOf(settings.triggerSymbolId);
            if (!math.HasReelSet(settings.reelSetId))
                throw new ArgumentException($"Free spins reel set '{settings.reelSetId}' does not exist.");
        }

        public void Apply(SpinContext context)
        {
            // During free spins only retriggers count.
            if (context.Request.IsFeatureSpin && !Settings.allowRetrigger) return;

            var positions = context.Grid.FindAll(_triggerSymbol);
            int spins = Settings.SpinsFor(positions.Count);
            if (spins > 0) context.Result.Set(new FreeSpinsResult(spins, positions));
        }

        public bool IsTriggered(SpinResult result) => result.Has<FreeSpinsResult>();

        public long Simulate(SpinResult trigger, SpinEngine engine)
        {
            if (!trigger.TryGet<FreeSpinsResult>(out var award)) return 0;

            var request = trigger.Request.ForFeature(Settings.reelSetId);
            int remaining = award.SpinsAwarded;
            int played = 0;
            long total = 0;

            while (remaining > 0 && played < Settings.maxTotalSpins)
            {
                var spin = engine.Spin(request);
                remaining--;
                played++;
                total += spin.TotalWin;
                if (spin.TryGet<FreeSpinsResult>(out var retrigger)) remaining += retrigger.SpinsAwarded;
            }

            return total;
        }
    }
}
