namespace SlotTemplate.Core.Random
{
    /// <summary>Default RNG backed by <see cref="System.Random"/>. Fine for prototyping, not for real-money play.</summary>
    public sealed class SystemRandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly System.Random _random;

        public SystemRandomNumberGenerator() => _random = new System.Random();
        public SystemRandomNumberGenerator(int seed) => _random = new System.Random(seed);

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
