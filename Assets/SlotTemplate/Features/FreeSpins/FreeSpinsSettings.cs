using System;

namespace SlotTemplate.Features.FreeSpins
{
    /// <summary>Tunable numbers for free spins. Plain data so the rule stays testable without Unity.</summary>
    [Serializable]
    public sealed class FreeSpinsSettings
    {
        /// <summary>Symbol id that triggers free spins, counted anywhere on the grid.</summary>
        public string triggerSymbolId = "SC";

        /// <summary>Spins awarded by trigger count: element 2 is for 3 symbols, element 4 for 5.</summary>
        public int[] spinsByCount = { 0, 0, 10, 15, 20 };

        /// <summary>Reel set used during free spins.</summary>
        public string reelSetId = "free";

        public bool allowRetrigger = true;

        /// <summary>Safety cap on total free spins in one feature.</summary>
        public int maxTotalSpins = 200;

        public int SpinsFor(int count) => count >= 1 && count <= spinsByCount.Length ? spinsByCount[count - 1] : 0;
    }
}
