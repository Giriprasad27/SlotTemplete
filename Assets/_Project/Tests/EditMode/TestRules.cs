using System.Collections.Generic;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Tests
{
    /// <summary>A tiny 3x3 machine used across tests.</summary>
    internal static class TestRules
    {
        public const int Cherry = 0;
        public const int Bell = 1;
        public const int Seven = 2;
        public const int Wild = 3;
        public const int Scatter = 4;

        public static Paytable CreatePaytable()
        {
            return new Paytable(new[]
            {
                new SymbolRule(Cherry, "Cherry", SymbolKind.Regular, new Dictionary<int, long> { { 3, 5 } }),
                new SymbolRule(Bell, "Bell", SymbolKind.Regular, new Dictionary<int, long> { { 3, 10 } }),
                new SymbolRule(Seven, "Seven", SymbolKind.Regular, new Dictionary<int, long> { { 2, 2 }, { 3, 50 } }),
                new SymbolRule(Wild, "Wild", SymbolKind.Wild, new Dictionary<int, long> { { 3, 100 } }),
                new SymbolRule(Scatter, "Scatter", SymbolKind.Scatter, new Dictionary<int, long> { { 2, 1 }, { 3, 5 } }),
            });
        }

        /// <summary>Three reels with identical strips; every symbol appears once.</summary>
        public static SlotRules Create()
        {
            var strip = new[] { Cherry, Bell, Seven, Wild, Scatter };
            var reels = new[] { new ReelStrip(strip), new ReelStrip(strip), new ReelStrip(strip) };
            var paylines = new[]
            {
                new Payline(0, new[] { 1, 1, 1 }), // middle
                new Payline(1, new[] { 0, 0, 0 }), // top
                new Payline(2, new[] { 2, 2, 2 }), // bottom
            };
            return new SlotRules(3, reels, paylines, CreatePaytable());
        }

        /// <summary>Builds a grid from rows written top to bottom, as they appear on screen.</summary>
        public static SpinGrid Grid(int[] top, int[] middle, int[] bottom)
        {
            var rows = new[] { top, middle, bottom };
            var symbols = new int[top.Length, rows.Length];
            for (int reel = 0; reel < top.Length; reel++)
            for (int row = 0; row < rows.Length; row++)
                symbols[reel, row] = rows[row][reel];

            return new SpinGrid(symbols, new int[top.Length]);
        }
    }
}
