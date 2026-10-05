using System;
using System.Collections.Generic;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core.Definition
{
    /// <summary>Validates <see cref="SlotDefinitionData"/> and converts it into <see cref="SlotMath"/>.</summary>
    public static class SlotMathFactory
    {
        public static SlotMath Build(SlotDefinitionData data)
        {
            var errors = Validate(data);
            if (errors.Count > 0)
                throw new InvalidOperationException("Slot definition is invalid:\n- " + string.Join("\n- ", errors));

            var symbols = new List<SymbolRule>(data.symbols.Count);
            for (int i = 0; i < data.symbols.Count; i++)
                symbols.Add(new SymbolRule(i, data.symbols[i].id, data.symbols[i].kind, data.symbols[i].pays));
            var paytable = new Paytable(symbols);

            var reelSets = new List<ReelSet>();
            foreach (var setData in data.reelSets)
            {
                var strips = new List<ReelStrip>();
                foreach (var reel in setData.reels)
                {
                    var ids = SplitStrip(reel);
                    var strip = new int[ids.Count];
                    for (int i = 0; i < ids.Count; i++) strip[i] = paytable.IndexOf(ids[i]);
                    strips.Add(new ReelStrip(strip));
                }
                reelSets.Add(new ReelSet(setData.id, strips));
            }

            var paylines = new List<Payline>(data.paylines.Count);
            for (int i = 0; i < data.paylines.Count; i++) paylines.Add(new Payline(i, data.paylines[i].rows));

            return new SlotMath(data.rows, reelSets, paylines, paytable);
        }

        /// <summary>Human-readable problems with the definition; empty when it is valid.</summary>
        public static List<string> Validate(SlotDefinitionData data)
        {
            var errors = new List<string>();
            if (data == null)
            {
                errors.Add("Definition is missing.");
                return errors;
            }

            if (data.rows <= 0) errors.Add("Rows must be at least 1.");
            if (data.symbols == null || data.symbols.Count == 0) errors.Add("Add at least one symbol.");
            if (data.reelSets == null || data.reelSets.Count == 0) errors.Add("Add at least one reel set.");
            if (data.paylines == null || data.paylines.Count == 0) errors.Add("Add at least one payline.");
            if (errors.Count > 0) return errors;

            var ids = new HashSet<string>();
            for (int i = 0; i < data.symbols.Count; i++)
            {
                var symbol = data.symbols[i];
                if (symbol == null || string.IsNullOrWhiteSpace(symbol.id)) errors.Add($"Symbol {i} needs an id.");
                else if (!ids.Add(symbol.id)) errors.Add($"Symbol id '{symbol.id}' is used twice.");
            }

            int reelCount = -1;
            var setIds = new HashSet<string>();
            foreach (var set in data.reelSets)
            {
                if (string.IsNullOrWhiteSpace(set.id)) { errors.Add("A reel set has no id."); continue; }
                if (!setIds.Add(set.id)) errors.Add($"Reel set id '{set.id}' is used twice.");

                if (set.reels == null || set.reels.Count == 0) { errors.Add($"Reel set '{set.id}' has no reels."); continue; }
                if (reelCount < 0) reelCount = set.reels.Count;
                else if (set.reels.Count != reelCount) errors.Add($"Reel set '{set.id}' has {set.reels.Count} reels; expected {reelCount}.");

                for (int r = 0; r < set.reels.Count; r++)
                {
                    var strip = SplitStrip(set.reels[r]);
                    if (strip.Count == 0) errors.Add($"Reel set '{set.id}' reel {r} is empty.");
                    foreach (var id in strip)
                    {
                        if (!ids.Contains(id)) errors.Add($"Reel set '{set.id}' reel {r} uses unknown symbol '{id}'.");
                    }
                }
            }

            for (int p = 0; p < data.paylines.Count; p++)
            {
                var rows = data.paylines[p]?.rows;
                if (rows == null || rows.Length != reelCount)
                {
                    errors.Add($"Payline {p} needs exactly {reelCount} rows (one per reel).");
                    continue;
                }
                foreach (var row in rows)
                {
                    if (row < 0 || row >= data.rows) errors.Add($"Payline {p} uses row {row}, outside 0..{data.rows - 1}.");
                }
            }

            return errors;
        }

        public static List<string> SplitStrip(string strip)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(strip)) return result;

            foreach (var part in strip.Split(','))
            {
                var id = part.Trim();
                if (id.Length > 0) result.Add(id);
            }
            return result;
        }
    }
}
