using System;
using System.Collections.Generic;

namespace SlotTemplate.Core.Random
{
    /// <summary>
    /// Returns a fixed sequence of values, looping when exhausted. Use it to force specific reel stops
    /// in tests or for debug "cheat" spins.
    /// </summary>
    public sealed class ScriptedRandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly int[] _values;
        private int _cursor;

        public ScriptedRandomNumberGenerator(params int[] values)
        {
            if (values == null || values.Length == 0) throw new ArgumentException("Provide at least one value.", nameof(values));
            _values = values;
        }

        public ScriptedRandomNumberGenerator(IReadOnlyList<int> values) : this(ToArray(values)) { }

        public int Next(int maxExclusive)
        {
            int value = _values[_cursor];
            _cursor = (_cursor + 1) % _values.Length;
            return ((value % maxExclusive) + maxExclusive) % maxExclusive;
        }

        private static int[] ToArray(IReadOnlyList<int> values)
        {
            var array = new int[values.Count];
            for (int i = 0; i < values.Count; i++) array[i] = values[i];
            return array;
        }
    }
}
