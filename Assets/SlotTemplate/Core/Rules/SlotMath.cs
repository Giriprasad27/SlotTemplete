using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>
    /// Validated, engine-independent game math built from <see cref="Definition.SlotDefinitionData"/>.
    /// </summary>
    public sealed class SlotMath
    {
        private readonly Dictionary<string, ReelSet> _reelSets;

        public int RowCount { get; }
        public int ReelCount { get; }
        public Paytable Paytable { get; }
        public IReadOnlyList<Payline> Paylines { get; }
        public IEnumerable<ReelSet> ReelSets => _reelSets.Values;

        public SlotMath(int rowCount, IEnumerable<ReelSet> reelSets, IReadOnlyList<Payline> paylines, Paytable paytable)
        {
            if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
            if (reelSets == null) throw new ArgumentNullException(nameof(reelSets));

            RowCount = rowCount;
            Paylines = paylines ?? throw new ArgumentNullException(nameof(paylines));
            Paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));

            _reelSets = new Dictionary<string, ReelSet>();
            foreach (var set in reelSets)
            {
                if (_reelSets.Count == 0) ReelCount = set.ReelCount;
                else if (set.ReelCount != ReelCount)
                    throw new ArgumentException($"Reel set '{set.Id}' has {set.ReelCount} reels; expected {ReelCount}.");
                _reelSets.Add(set.Id, set);
            }

            if (_reelSets.Count == 0) throw new ArgumentException("At least one reel set is required.");
        }

        public ReelSet GetReelSet(string id) =>
            _reelSets.TryGetValue(id, out var set) ? set : throw new KeyNotFoundException($"Unknown reel set '{id}'.");

        public bool HasReelSet(string id) => _reelSets.ContainsKey(id);
    }
}
