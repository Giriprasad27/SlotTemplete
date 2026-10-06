using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>All symbol rules, indexed by symbol index and looked up by id.</summary>
    public sealed class Paytable
    {
        private readonly SymbolRule[] _symbols;
        private readonly Dictionary<string, int> _indexById = new Dictionary<string, int>();

        public IReadOnlyList<SymbolRule> Symbols => _symbols;
        public int Count => _symbols.Length;

        public Paytable(IReadOnlyList<SymbolRule> symbols)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));

            _symbols = new SymbolRule[symbols.Count];
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i].Index != i)
                    throw new ArgumentException($"Symbol '{symbols[i].Id}' has index {symbols[i].Index}; it must match its position {i}.");
                _symbols[i] = symbols[i];
                _indexById[symbols[i].Id] = i;
            }
        }

        public SymbolRule this[int index] => _symbols[index];

        public int IndexOf(string id) =>
            _indexById.TryGetValue(id, out var index) ? index : throw new KeyNotFoundException($"Unknown symbol id '{id}'.");

        public bool TryIndexOf(string id, out int index) => _indexById.TryGetValue(id, out index);
    }
}
