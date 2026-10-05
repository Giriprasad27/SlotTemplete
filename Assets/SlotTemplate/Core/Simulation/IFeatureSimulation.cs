using SlotTemplate.Core.Spin;

namespace SlotTemplate.Core.Simulation
{
    /// <summary>
    /// Implemented by feature rules so the RTP simulator can play the feature out with math only,
    /// no presentation. Return the feature's total win for the given trigger, or 0 if it did not trigger.
    /// </summary>
    public interface IFeatureSimulation
    {
        string FeatureId { get; }
        bool IsTriggered(SpinResult result);
        long Simulate(SpinResult trigger, SpinEngine engine);
    }
}
