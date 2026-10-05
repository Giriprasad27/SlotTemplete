using System.Collections.Generic;

namespace SlotTemplate.Core.Model
{
    /// <summary>A win from scatter symbols landing anywhere on the grid.</summary>
    public sealed class ScatterWin
    {
        public int SymbolId { get; }
        public int Count { get; }
        public long Payout { get; }
        public IReadOnlyList<GridPosition> Positions { get; }

        public ScatterWin(int symbolId, int count, long payout, IReadOnlyList<GridPosition> positions)
        {
            SymbolId = symbolId;
            Count = count;
            Payout = payout;
            Positions = positions;
        }

        public override string ToString() => $"Scatter: {Count}x symbol {SymbolId} pays {Payout}";
    }
}
