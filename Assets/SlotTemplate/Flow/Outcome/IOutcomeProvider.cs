using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Flow.Outcome
{
    /// <summary>
    /// Decides spin results. The game only asks "give me a result" and never cares who answers:
    /// local math today, a server tomorrow, or a debug provider forcing outcomes.
    /// </summary>
    public interface IOutcomeProvider
    {
        UniTask<SpinResult> Spin(SpinRequest request, CancellationToken ct);

        /// <summary>Rebuilds a saved result from its stops, for crash recovery.</summary>
        UniTask<SpinResult> Restore(SpinRequest request, IReadOnlyList<int> stops, CancellationToken ct);
    }
}
