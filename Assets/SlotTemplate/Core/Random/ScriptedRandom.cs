using System;

namespace SlotTemplate.Core.Random
{
    /// <summary>Returns a fixed sequence of values, looping when exhausted. For tests and forced outcomes.</summary>
    public sealed class ScriptedRandom : IRandomNumberGenerator
    {
        private readonly int[] _values;
        private int _cursor;

        public ScriptedRandom(params int[] values)
        {
            if (values == null || values.Length == 0) throw new ArgumentException("Provide at least one value.", nameof(values));
            _values = values;
        }

        public int Next(int maxExclusive)
        {
            int value = _values[_cursor];
            _cursor = (_cursor + 1) % _values.Length;
            return ((value % maxExclusive) + maxExclusive) % maxExclusive;
        }
    }
}
