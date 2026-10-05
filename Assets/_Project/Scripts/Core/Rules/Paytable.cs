using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>All symbol rules, indexed by symbol id.</summary>
    public sealed class Paytable
    {
        private readonly SymbolRule[] _symbols;

        public IReadOnlyList<SymbolRule> Symbols => _symbols;
        public int Count => _symbols.Length;

        public Paytable(IReadOnlyList<SymbolRule> symbols)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));

            _symbols = new SymbolRule[symbols.Count];
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i].Id != i)
                    throw new ArgumentException($"Symbol at index {i} has id {symbols[i].Id}; ids must match their index.");
                _symbols[i] = symbols[i];
            }
        }

        public SymbolRule this[int id] => _symbols[id];
    }
}
