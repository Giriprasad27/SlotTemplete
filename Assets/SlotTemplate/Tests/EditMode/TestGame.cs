using System.Collections.Generic;
using SlotTemplate.Core.Definition;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Tests
{
    /// <summary>A tiny 3x3 machine used across tests. Every strip is "CH,BE,SEV,WILD,SC".</summary>
    internal static class TestGame
    {
        public const int Cherry = 0;
        public const int Bell = 1;
        public const int Seven = 2;
        public const int Wild = 3;
        public const int Scatter = 4;

        public static SlotDefinitionData Data()
        {
            var data = new SlotDefinitionData { rows = 3 };
            data.symbols.Add(new SymbolData("CH", SymbolKind.Regular, 0, 0, 5));
            data.symbols.Add(new SymbolData("BE", SymbolKind.Regular, 0, 0, 10));
            data.symbols.Add(new SymbolData("SEV", SymbolKind.Regular, 0, 2, 50));
            data.symbols.Add(new SymbolData("WILD", SymbolKind.Wild, 0, 0, 100));
            data.symbols.Add(new SymbolData("SC", SymbolKind.Scatter, 0, 1, 5));

            const string strip = "CH,BE,SEV,WILD,SC";
            data.reelSets.Add(new ReelSetData("base", strip, strip, strip));
            // Free spins strip: no scatter, so free spins never retrigger in tests unless asked to.
            data.reelSets.Add(new ReelSetData("free", "CH,BE,SEV", "CH,BE,SEV", "CH,BE,SEV"));

            data.paylines.Add(new PaylineData(1, 1, 1)); // middle
            data.paylines.Add(new PaylineData(0, 0, 0)); // top
            data.paylines.Add(new PaylineData(2, 2, 2)); // bottom
            return data;
        }

        public static SlotMath Math() => SlotMathFactory.Build(Data());

        public static List<ISpinRule> PayRules() => new List<ISpinRule> { new PaylineRule(), new ScatterPayRule() };

        /// <summary>Evaluates a hand-written grid (rows top to bottom) with the pay rules.</summary>
        public static SpinResult Evaluate(int[] top, int[] middle, int[] bottom, long lineBet = 1)
        {
            var rows = new[] { top, middle, bottom };
            var symbols = new int[top.Length, 3];
            for (int reel = 0; reel < top.Length; reel++)
            for (int row = 0; row < 3; row++)
                symbols[reel, row] = rows[row][reel];

            var math = Math();
            var request = new SpinRequest(lineBet, math.Paylines.Count);
            var grid = new Grid("base", symbols, new int[top.Length]);
            var result = new SpinResult(request, grid);
            var context = new SpinContext(math, request, grid, result);
            foreach (var rule in PayRules()) rule.Apply(context);
            return result;
        }
    }
}
