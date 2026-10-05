using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>A named set of strips, one per reel. Features can spin on their own set.</summary>
    public sealed class ReelSet
    {
        public string Id { get; }
        public IReadOnlyList<ReelStrip> Reels { get; }
        public int ReelCount => Reels.Count;

        public ReelSet(string id, IReadOnlyList<ReelStrip> reels)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Reels = reels ?? throw new ArgumentNullException(nameof(reels));
        }
    }
}
