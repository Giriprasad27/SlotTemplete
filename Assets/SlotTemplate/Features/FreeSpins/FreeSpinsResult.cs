using System.Collections.Generic;
using SlotTemplate.Core.Model;

namespace SlotTemplate.Features.FreeSpins
{
    /// <summary>Written to a spin result by <see cref="FreeSpinsRule"/> when free spins trigger.</summary>
    public sealed class FreeSpinsResult
    {
        public int SpinsAwarded { get; }
        public IReadOnlyList<GridPosition> TriggerPositions { get; }

        public FreeSpinsResult(int spinsAwarded, IReadOnlyList<GridPosition> triggerPositions)
        {
            SpinsAwarded = spinsAwarded;
            TriggerPositions = triggerPositions;
        }
    }
}
