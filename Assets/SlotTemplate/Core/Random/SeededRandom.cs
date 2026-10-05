using System;

namespace SlotTemplate.Core.Random
{
    /// <summary>
    /// Fast seeded PRNG (xoshiro128**), identical on every platform so a seed always replays the same
    /// spins. Suitable for play money and simulation; real-money games need a certified RNG.
    /// </summary>
    public sealed class SeededRandom : IRandomNumberGenerator
    {
        private uint _s0, _s1, _s2, _s3;

        public SeededRandom() : this(Environment.TickCount) { }

        public SeededRandom(int seed)
        {
            // SplitMix64 expands the seed into four non-zero state words.
            ulong x = (ulong)seed;
            ulong NextSplit()
            {
                x += 0x9E3779B97F4A7C15UL;
                ulong z = x;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }

            ulong a = NextSplit(), b = NextSplit();
            _s0 = (uint)a; _s1 = (uint)(a >> 32); _s2 = (uint)b; _s3 = (uint)(b >> 32);
            if ((_s0 | _s1 | _s2 | _s3) == 0) _s0 = 1;
        }

        public uint NextUInt()
        {
            uint result = RotateLeft(_s1 * 5, 7) * 9;
            uint t = _s1 << 9;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotateLeft(_s3, 11);
            return result;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));

            // Rejection sampling avoids modulo bias.
            uint bound = (uint)maxExclusive;
            uint threshold = (uint)(-(int)bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        private static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));
    }
}
