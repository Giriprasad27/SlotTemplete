using System.Collections.Generic;

namespace SlotTemplate.Core.Model
{
    /// <summary>A win on a single payline.</summary>
    public sealed class LineWin
    {
        public int PaylineIndex { get; }
        public int SymbolId { get; }
        public int Count { get; }
        public long Payout { get; }

        /// <summary>The cells that make up the winning combination, left to right.</summary>
        public IReadOnlyList<GridPosition> Positions { get; }

        public LineWin(int paylineIndex, int symbolId, int count, long payout, IReadOnlyList<GridPosition> positions)
        {
            PaylineIndex = paylineIndex;
            SymbolId = symbolId;
            Count = count;
            Payout = payout;
            Positions = positions;
        }

        public override string ToString() => $"Line {PaylineIndex}: {Count}x symbol {SymbolId} pays {Payout}";
    }
}
