using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>The looping sequence of symbols printed on one reel.</summary>
    public sealed class ReelStrip
    {
        private readonly int[] _symbols;

        public int Length => _symbols.Length;
        public IReadOnlyList<int> Symbols => _symbols;

        public ReelStrip(IReadOnlyList<int> symbols)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));
            if (symbols.Count == 0) throw new ArgumentException("A reel strip needs at least one symbol.", nameof(symbols));

            _symbols = new int[symbols.Count];
            for (int i = 0; i < symbols.Count; i++) _symbols[i] = symbols[i];
        }

        /// <summary>Symbol at any index; wraps in both directions.</summary>
        public int GetSymbol(int index) => _symbols[Wrap(index)];

        public int Wrap(int index)
        {
            int wrapped = index % _symbols.Length;
            return wrapped < 0 ? wrapped + _symbols.Length : wrapped;
        }
    }
}
