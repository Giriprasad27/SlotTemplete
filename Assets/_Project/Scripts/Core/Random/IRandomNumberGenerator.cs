namespace SlotTemplate.Core.Random
{
    /// <summary>Source of randomness for spins. Swap in a certified RNG or a scripted one for tests.</summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>Returns a value in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }
}
