using System.Collections.Generic;
using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Spin
{
    public enum WinKind
    {
        Line,
        Scatter,
    }

    /// <summary>One paying combination.</summary>
    public sealed class Win
    {
        public WinKind Kind { get; }
        public int SymbolIndex { get; }
        public int Count { get; }
        public long Payout { get; }

        /// <summary>Payline index for line wins; -1 otherwise.</summary>
        public int PaylineIndex { get; }

        public IReadOnlyList<GridPosition> Positions { get; }

        public Win(WinKind kind, int symbolIndex, int count, long payout, IReadOnlyList<GridPosition> positions, int paylineIndex = -1)
        {
            Kind = kind;
            SymbolIndex = symbolIndex;
            Count = count;
            Payout = payout;
            Positions = positions;
            PaylineIndex = paylineIndex;
        }

        public override string ToString() =>
            Kind == WinKind.Line
                ? $"Line {PaylineIndex}: {Count}x symbol {SymbolIndex} pays {Payout}"
                : $"Scatter: {Count}x symbol {SymbolIndex} pays {Payout}";
    }
}
