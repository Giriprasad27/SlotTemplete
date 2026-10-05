using System;
using System.Collections.Generic;
using System.Text;

namespace SlotTemplate.Core.Model
{
    /// <summary>
    /// The symbols visible after a spin, plus the strip stop index of each reel.
    /// Symbols are referenced by their index into the paytable.
    /// </summary>
    public sealed class SpinGrid
    {
        private readonly int[,] _symbols;
        private readonly int[] _stops;

        public int ReelCount { get; }
        public int RowCount { get; }

        /// <summary>Strip index shown in the top row of each reel.</summary>
        public IReadOnlyList<int> Stops => _stops;

        public SpinGrid(int[,] symbols, int[] stops)
        {
            _symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
            _stops = stops ?? throw new ArgumentNullException(nameof(stops));
            ReelCount = symbols.GetLength(0);
            RowCount = symbols.GetLength(1);

            if (stops.Length != ReelCount)
                throw new ArgumentException("One stop index is required per reel.", nameof(stops));
        }

        public int this[int reel, int row] => _symbols[reel, row];
        public int this[GridPosition position] => _symbols[position.Reel, position.Row];

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int row = 0; row < RowCount; row++)
            {
                for (int reel = 0; reel < ReelCount; reel++)
                    sb.Append(_symbols[reel, row].ToString().PadLeft(3));
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
