using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>
    /// The complete, engine-independent definition of a slot machine's math.
    /// Built from ScriptableObject config at runtime, or directly in tests.
    /// </summary>
    public sealed class SlotRules
    {
        public int RowCount { get; }
        public IReadOnlyList<ReelStrip> Reels { get; }
        public IReadOnlyList<Payline> Paylines { get; }
        public Paytable Paytable { get; }

        public int ReelCount => Reels.Count;

        public SlotRules(int rowCount, IReadOnlyList<ReelStrip> reels, IReadOnlyList<Payline> paylines, Paytable paytable)
        {
            if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
            Reels = reels ?? throw new ArgumentNullException(nameof(reels));
            Paylines = paylines ?? throw new ArgumentNullException(nameof(paylines));
            Paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));
            RowCount = rowCount;

            Validate();
        }

        private void Validate()
        {
            if (Reels.Count == 0) throw new ArgumentException("At least one reel is required.");
            if (Paylines.Count == 0) throw new ArgumentException("At least one payline is required.");

            foreach (var reel in Reels)
            {
                foreach (var symbol in reel.Symbols)
                {
                    if (symbol < 0 || symbol >= Paytable.Count)
                        throw new ArgumentException($"Reel strip references unknown symbol id {symbol}.");
                }
            }

            foreach (var line in Paylines)
            {
                if (line.Rows.Count != Reels.Count)
                    throw new ArgumentException($"Payline {line.Index} has {line.Rows.Count} rows but there are {Reels.Count} reels.");

                foreach (var row in line.Rows)
                {
                    if (row < 0 || row >= RowCount)
                        throw new ArgumentException($"Payline {line.Index} uses row {row}, outside 0..{RowCount - 1}.");
                }
            }
        }
    }
}
