using System;
using System.Collections.Generic;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core.Evaluation
{
    /// <summary>
    /// Classic left-to-right payline evaluation with wild substitution, plus scatter pays anywhere.
    /// Each payline pays its single best combination.
    /// </summary>
    public sealed class PaylineWinEvaluator : IWinEvaluator
    {
        private const int NoSymbol = -1;

        private readonly SlotRules _rules;

        public PaylineWinEvaluator(SlotRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public SpinOutcome Evaluate(SpinGrid grid, long lineBet, long totalBet)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (grid.ReelCount != _rules.ReelCount || grid.RowCount != _rules.RowCount)
                throw new ArgumentException("Grid size does not match the rules.", nameof(grid));

            var lineWins = new List<LineWin>();
            foreach (var payline in _rules.Paylines)
            {
                var win = EvaluateLine(grid, payline, lineBet);
                if (win != null) lineWins.Add(win);
            }

            var scatterWins = EvaluateScatters(grid, totalBet);
            return new SpinOutcome(grid, lineBet, totalBet, lineWins, scatterWins);
        }

        private LineWin EvaluateLine(SpinGrid grid, Payline payline, long lineBet)
        {
            var paytable = _rules.Paytable;
            int reelCount = grid.ReelCount;

            int firstSymbol = grid[0, payline.RowOnReel(0)];
            if (paytable[firstSymbol].Kind == SymbolKind.Scatter) return null;

            // Leading wilds can pay on their own (e.g. 3 wilds) or extend a regular symbol's run.
            int leadingWilds = 0;
            int target = NoSymbol;
            int runLength = 0;

            for (int reel = 0; reel < reelCount; reel++)
            {
                int symbol = grid[reel, payline.RowOnReel(reel)];
                var kind = paytable[symbol].Kind;

                if (kind == SymbolKind.Scatter) break;

                if (kind == SymbolKind.Wild)
                {
                    if (target == NoSymbol) leadingWilds++;
                    runLength++;
                    continue;
                }

                if (target == NoSymbol) target = symbol;
                else if (symbol != target) break;

                runLength++;
            }

            long wildMultiplier = leadingWilds > 0 ? paytable[firstSymbol].GetMultiplier(leadingWilds) : 0;
            long targetMultiplier = target != NoSymbol ? paytable[target].GetMultiplier(runLength) : 0;

            if (wildMultiplier <= 0 && targetMultiplier <= 0) return null;

            bool wildsPayMore = wildMultiplier > targetMultiplier;
            int count = wildsPayMore ? leadingWilds : runLength;
            int paidSymbol = wildsPayMore ? firstSymbol : target;
            long payout = (wildsPayMore ? wildMultiplier : targetMultiplier) * lineBet;

            var positions = new GridPosition[count];
            for (int reel = 0; reel < count; reel++)
                positions[reel] = new GridPosition(reel, payline.RowOnReel(reel));

            return new LineWin(payline.Index, paidSymbol, count, payout, positions);
        }

        private List<ScatterWin> EvaluateScatters(SpinGrid grid, long totalBet)
        {
            var wins = new List<ScatterWin>();

            foreach (var rule in _rules.Paytable.Symbols)
            {
                if (rule.Kind != SymbolKind.Scatter) continue;

                var positions = new List<GridPosition>();
                for (int reel = 0; reel < grid.ReelCount; reel++)
                for (int row = 0; row < grid.RowCount; row++)
                {
                    if (grid[reel, row] == rule.Id) positions.Add(new GridPosition(reel, row));
                }

                long multiplier = rule.GetMultiplier(positions.Count);
                if (multiplier > 0)
                    wins.Add(new ScatterWin(rule.Id, positions.Count, multiplier * totalBet, positions));
            }

            return wins;
        }
    }
}
