using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Rules
{
    /// <summary>Pay rules for one symbol.</summary>
    public sealed class SymbolRule
    {
        private readonly long[] _pays;

        public int Index { get; }
        public string Id { get; }
        public SymbolKind Kind { get; }

        public SymbolRule(int index, string id, SymbolKind kind, long[] pays)
        {
            Index = index;
            Id = id;
            Kind = kind;
            _pays = pays != null ? (long[])pays.Clone() : new long[0];
        }

        /// <summary>Multiplier for <paramref name="count"/> matching symbols, or 0 when that count does not pay.</summary>
        public long GetMultiplier(int count) => count >= 1 && count <= _pays.Length ? _pays[count - 1] : 0;
    }
}
