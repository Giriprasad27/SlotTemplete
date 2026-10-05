using System.Collections.Generic;
using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Rules
{
    /// <summary>Pay rules for one symbol. Payouts are multipliers keyed by match count.</summary>
    public sealed class SymbolRule
    {
        private readonly Dictionary<int, long> _payouts;

        public int Id { get; }
        public string Name { get; }
        public SymbolKind Kind { get; }

        public SymbolRule(int id, string name, SymbolKind kind, IDictionary<int, long> payouts)
        {
            Id = id;
            Name = name;
            Kind = kind;
            _payouts = payouts != null ? new Dictionary<int, long>(payouts) : new Dictionary<int, long>();
        }

        /// <summary>
        /// Multiplier for a run of <paramref name="count"/> symbols. Line wins multiply the line bet;
        /// scatter wins multiply the total bet. Returns 0 when the count does not pay.
        /// </summary>
        public long GetMultiplier(int count) => _payouts.TryGetValue(count, out var value) ? value : 0;
    }
}
