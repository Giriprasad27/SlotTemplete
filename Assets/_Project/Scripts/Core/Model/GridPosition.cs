using System;

namespace SlotTemplate.Core.Model
{
    /// <summary>A cell on the visible grid. Row 0 is the top row.</summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int Reel;
        public readonly int Row;

        public GridPosition(int reel, int row)
        {
            Reel = reel;
            Row = row;
        }

        public bool Equals(GridPosition other) => Reel == other.Reel && Row == other.Row;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => (Reel * 397) ^ Row;
        public override string ToString() => $"({Reel},{Row})";
    }
}
