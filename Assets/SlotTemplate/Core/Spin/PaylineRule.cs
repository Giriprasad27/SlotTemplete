using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core.Spin
{
    /// <summary>
    /// Left-to-right payline wins with wild substitution. Each payline pays its single best combination:
    /// either the leading wilds on their own or the run they extend.
    /// </summary>
    public sealed class PaylineRule : ISpinRule
    {
        private const int NoSymbol = -1;

        public void Apply(SpinContext context)
        {
            var paylines = context.Math.Paylines;
            int lineCount = System.Math.Min(context.Request.LineCount, paylines.Count);

            for (int i = 0; i < lineCount; i++)
            {
                var win = Evaluate(context, paylines[i]);
                if (win != null) context.Result.AddWin(win);
            }
        }

        private static Win Evaluate(SpinContext context, Payline payline)
        {
            var grid = context.Grid;
            var paytable = context.Math.Paytable;

            int first = grid[0, payline.RowOnReel(0)];
            if (paytable[first].Kind == SymbolKind.Scatter) return null;

            int leadingWilds = 0;
            int target = NoSymbol;
            int run = 0;

            for (int reel = 0; reel < grid.ReelCount; reel++)
            {
                int symbol = grid[reel, payline.RowOnReel(reel)];
                var kind = paytable[symbol].Kind;

                if (kind == SymbolKind.Scatter) break;
                if (kind == SymbolKind.Wild)
                {
                    if (target == NoSymbol) leadingWilds++;
                    run++;
                    continue;
                }

                if (target == NoSymbol) target = symbol;
                else if (symbol != target) break;
                run++;
            }

            long wildMultiplier = leadingWilds > 0 ? paytable[first].GetMultiplier(leadingWilds) : 0;
            long targetMultiplier = target != NoSymbol ? paytable[target].GetMultiplier(run) : 0;
            if (wildMultiplier <= 0 && targetMultiplier <= 0) return null;

            bool wildsPayMore = wildMultiplier > targetMultiplier;
            int count = wildsPayMore ? leadingWilds : run;
            int symbolIndex = wildsPayMore ? first : target;
            long payout = (wildsPayMore ? wildMultiplier : targetMultiplier) * context.Request.LineBet;

            var positions = new GridPosition[count];
            for (int reel = 0; reel < count; reel++)
                positions[reel] = new GridPosition(reel, payline.RowOnReel(reel));

            return new Win(WinKind.Line, symbolIndex, count, payout, positions, payline.Index);
        }
    }
}
