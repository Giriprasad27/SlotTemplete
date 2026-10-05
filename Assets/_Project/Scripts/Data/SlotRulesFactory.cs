using System;
using System.Collections.Generic;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Data
{
    /// <summary>Converts designer config into the engine-free rules the game logic runs on.</summary>
    public static class SlotRulesFactory
    {
        public static SlotRules Build(SlotMachineConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var errors = Validate(config);
            if (errors.Count > 0)
                throw new InvalidOperationException($"Slot config '{config.name}' is invalid:\n- " + string.Join("\n- ", errors));

            var symbolIds = BuildSymbolIndex(config);

            var rules = new List<SymbolRule>(config.Symbols.Count);
            for (int id = 0; id < config.Symbols.Count; id++)
            {
                var symbol = config.Symbols[id];
                var payouts = new Dictionary<int, long>();
                foreach (var payout in symbol.Payouts) payouts[payout.count] = payout.multiplier;
                rules.Add(new SymbolRule(id, symbol.DisplayName, symbol.Kind, payouts));
            }

            var reels = new List<ReelStrip>(config.Reels.Count);
            foreach (var reel in config.Reels)
            {
                var strip = new int[reel.symbols.Count];
                for (int i = 0; i < strip.Length; i++) strip[i] = symbolIds[reel.symbols[i]];
                reels.Add(new ReelStrip(strip));
            }

            var paylines = new List<Payline>(config.Paylines.Count);
            for (int i = 0; i < config.Paylines.Count; i++)
                paylines.Add(new Payline(i, config.Paylines[i].rows));

            return new SlotRules(config.RowCount, reels, paylines, new Paytable(rules));
        }

        /// <summary>Returns human-readable problems with the config; empty when it is valid.</summary>
        public static List<string> Validate(SlotMachineConfig config)
        {
            var errors = new List<string>();

            if (config.Symbols.Count == 0) errors.Add("Add at least one symbol.");
            if (config.Reels.Count == 0) errors.Add("Add at least one reel.");
            if (config.Paylines.Count == 0) errors.Add("Add at least one payline.");
            if (config.LineBetLevels == null || config.LineBetLevels.Count == 0) errors.Add("Add at least one line bet level.");

            var known = new HashSet<SymbolDefinition>();
            for (int i = 0; i < config.Symbols.Count; i++)
            {
                if (config.Symbols[i] == null) errors.Add($"Symbol slot {i} is empty.");
                else if (!known.Add(config.Symbols[i])) errors.Add($"Symbol '{config.Symbols[i].name}' is listed twice.");
            }

            for (int r = 0; r < config.Reels.Count; r++)
            {
                var strip = config.Reels[r].symbols;
                if (strip.Count == 0) errors.Add($"Reel {r} has an empty strip.");

                foreach (var symbol in strip)
                {
                    if (symbol == null) errors.Add($"Reel {r} has an empty strip slot.");
                    else if (!known.Contains(symbol)) errors.Add($"Reel {r} uses '{symbol.name}', which is not in the Symbols list.");
                }
            }

            for (int p = 0; p < config.Paylines.Count; p++)
            {
                var rows = config.Paylines[p].rows;
                if (rows == null || rows.Length != config.Reels.Count)
                {
                    errors.Add($"Payline {p} needs exactly {config.Reels.Count} rows (one per reel).");
                    continue;
                }

                foreach (var row in rows)
                {
                    if (row < 0 || row >= config.RowCount)
                        errors.Add($"Payline {p} uses row {row}, outside 0..{config.RowCount - 1}.");
                }
            }

            return errors;
        }

        private static Dictionary<SymbolDefinition, int> BuildSymbolIndex(SlotMachineConfig config)
        {
            var index = new Dictionary<SymbolDefinition, int>();
            for (int i = 0; i < config.Symbols.Count; i++) index[config.Symbols[i]] = i;
            return index;
        }
    }
}
