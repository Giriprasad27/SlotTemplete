using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Evaluation
{
    /// <summary>Turns a grid of symbols into wins. Implement this for ways-to-win, cluster pays, etc.</summary>
    public interface IWinEvaluator
    {
        SpinOutcome Evaluate(SpinGrid grid, long lineBet, long totalBet);
    }
}
