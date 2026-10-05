using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core.Spin
{
    /// <summary>What a rule can read (math, request, grid) and where it writes (result).</summary>
    public sealed class SpinContext
    {
        public SlotMath Math { get; }
        public SpinRequest Request { get; }
        public Grid Grid { get; }
        public SpinResult Result { get; }

        public SpinContext(SlotMath math, SpinRequest request, Grid grid, SpinResult result)
        {
            Math = math;
            Request = request;
            Grid = grid;
            Result = result;
        }
    }
}
