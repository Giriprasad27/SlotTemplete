using System;

namespace SlotTemplate.Core.Spin
{
    /// <summary>What to spin: the bet and which reel set to use.</summary>
    public sealed class SpinRequest
    {
        public const string BaseReelSet = "base";

        public long LineBet { get; }
        public int LineCount { get; }
        public string ReelSetId { get; }

        /// <summary>True for spins awarded by a feature (e.g. free spins); these are not paid for.</summary>
        public bool IsFeatureSpin { get; }

        public long TotalBet => LineBet * LineCount;

        public SpinRequest(long lineBet, int lineCount, string reelSetId = BaseReelSet, bool isFeatureSpin = false)
        {
            if (lineBet <= 0) throw new ArgumentOutOfRangeException(nameof(lineBet));
            if (lineCount <= 0) throw new ArgumentOutOfRangeException(nameof(lineCount));

            LineBet = lineBet;
            LineCount = lineCount;
            ReelSetId = reelSetId ?? BaseReelSet;
            IsFeatureSpin = isFeatureSpin;
        }

        /// <summary>The same bet, spun for free on another reel set.</summary>
        public SpinRequest ForFeature(string reelSetId) => new SpinRequest(LineBet, LineCount, reelSetId, true);
    }
}
