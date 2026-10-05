namespace SlotTemplate.Core.Model
{
    /// <summary>How a symbol behaves during win evaluation.</summary>
    public enum SymbolKind
    {
        /// <summary>Pays only when it lines up with itself.</summary>
        Regular = 0,

        /// <summary>Substitutes for any regular symbol on a payline, and can pay on its own.</summary>
        Wild = 1,

        /// <summary>Pays anywhere on the grid, independent of paylines.</summary>
        Scatter = 2,
    }
}
