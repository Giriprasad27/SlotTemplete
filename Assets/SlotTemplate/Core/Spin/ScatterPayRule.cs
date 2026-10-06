using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Spin
{
    /// <summary>Scatter symbols pay anywhere on the grid, as a multiple of the total bet.</summary>
    public sealed class ScatterPayRule : ISpinRule
    {
        public void Apply(SpinContext context)
        {
            foreach (var symbol in context.Math.Paytable.Symbols)
            {
                if (symbol.Kind != SymbolKind.Scatter) continue;

                var positions = context.Grid.FindAll(symbol.Index);
                long multiplier = symbol.GetMultiplier(positions.Count);
                if (multiplier > 0)
                    context.Result.AddWin(new Win(WinKind.Scatter, symbol.Index, positions.Count, multiplier * context.Request.TotalBet, positions));
            }
        }
    }
}
