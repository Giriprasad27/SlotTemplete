using System.Collections.Generic;

namespace SlotTemplate.Core.Model
{
    /// <summary>Everything the presentation layer needs to show one spin.</summary>
    public sealed class SpinOutcome
    {
        public SpinGrid Grid { get; }
        public long TotalBet { get; }
        public long LineBet { get; }
        public IReadOnlyList<LineWin> LineWins { get; }
        public IReadOnlyList<ScatterWin> ScatterWins { get; }
        public long TotalWin { get; }

        public bool IsWin => TotalWin > 0;

        public SpinOutcome(SpinGrid grid, long lineBet, long totalBet,
            IReadOnlyList<LineWin> lineWins, IReadOnlyList<ScatterWin> scatterWins)
        {
            Grid = grid;
            LineBet = lineBet;
            TotalBet = totalBet;
            LineWins = lineWins;
            ScatterWins = scatterWins;

            long total = 0;
            foreach (var win in lineWins) total += win.Payout;
            foreach (var win in scatterWins) total += win.Payout;
            TotalWin = total;
        }
    }
}
