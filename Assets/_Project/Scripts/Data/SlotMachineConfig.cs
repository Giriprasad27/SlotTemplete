using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotTemplate.Data
{
    /// <summary>
    /// Designer-facing definition of a machine: symbols, reel strips, paylines and economy.
    /// Converted to engine-free <see cref="Core.Rules.SlotRules"/> by <see cref="SlotRulesFactory"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Slot Template/Slot Machine Config", order = 1)]
    public sealed class SlotMachineConfig : ScriptableObject
    {
        [Serializable]
        public sealed class ReelStripData
        {
            [Tooltip("Symbols in order, top to bottom. Repeat a symbol to make it more likely.")]
            public List<SymbolDefinition> symbols = new List<SymbolDefinition>();
        }

        [Serializable]
        public sealed class PaylineData
        {
            public Color color = Color.yellow;

            [Tooltip("Row index on each reel, left to right. 0 is the top row.")]
            public int[] rows = Array.Empty<int>();
        }

        [Header("Layout")]
        [Min(1)] [SerializeField] private int rowCount = 3;

        [Header("Symbols")]
        [Tooltip("Every symbol the machine can show. Order defines the symbol ids used by the game logic.")]
        [SerializeField] private List<SymbolDefinition> symbols = new List<SymbolDefinition>();

        [Header("Reels")]
        [SerializeField] private List<ReelStripData> reels = new List<ReelStripData>();

        [Header("Paylines")]
        [SerializeField] private List<PaylineData> paylines = new List<PaylineData>();

        [Header("Economy")]
        [SerializeField] private long[] lineBetLevels = { 1, 2, 5, 10 };
        [Min(0)] [SerializeField] private int defaultBetLevel;
        [Min(0)] [SerializeField] private long startingBalance = 1000;

        [Header("Random")]
        [Tooltip("Use a fixed seed so every play session produces the same spins. Handy for debugging.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 12345;

        public int RowCount => rowCount;
        public int ReelCount => reels.Count;
        public IReadOnlyList<SymbolDefinition> Symbols => symbols;
        public IReadOnlyList<ReelStripData> Reels => reels;
        public IReadOnlyList<PaylineData> Paylines => paylines;
        public IReadOnlyList<long> LineBetLevels => lineBetLevels;
        public int DefaultBetLevel => defaultBetLevel;
        public long StartingBalance => startingBalance;
        public bool UseFixedSeed => useFixedSeed;
        public int Seed => seed;

#if UNITY_EDITOR
        /// <summary>Editor-only setup used by the sample scene builder.</summary>
        public void EditorSetup(int newRowCount, List<SymbolDefinition> newSymbols, List<ReelStripData> newReels,
            List<PaylineData> newPaylines, long[] newLineBetLevels, long newStartingBalance)
        {
            rowCount = newRowCount;
            symbols = newSymbols;
            reels = newReels;
            paylines = newPaylines;
            lineBetLevels = newLineBetLevels;
            startingBalance = newStartingBalance;
        }

        private void OnValidate()
        {
            foreach (var error in SlotRulesFactory.Validate(this))
                Debug.LogWarning($"[{name}] {error}", this);
        }
#endif
    }
}
