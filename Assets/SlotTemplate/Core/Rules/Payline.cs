using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Rules
{
    /// <summary>A path across the reels: the row used on each reel, left to right.</summary>
    public sealed class Payline
    {
        private readonly int[] _rows;

        public int Index { get; }
        public IReadOnlyList<int> Rows => _rows;

        public Payline(int index, IReadOnlyList<int> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            Index = index;
            _rows = new int[rows.Count];
            for (int i = 0; i < rows.Count; i++) _rows[i] = rows[i];
        }

        public int RowOnReel(int reel) => _rows[reel];
    }
}
