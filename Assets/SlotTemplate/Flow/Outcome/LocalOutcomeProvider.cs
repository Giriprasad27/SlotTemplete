using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Flow.Outcome
{
    /// <summary>Runs the spin math on the device.</summary>
    public sealed class LocalOutcomeProvider : IOutcomeProvider
    {
        private readonly SpinEngine _engine;

        public LocalOutcomeProvider(SpinEngine engine) => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

        public UniTask<SpinResult> Spin(SpinRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(_engine.Spin(request));
        }

        public UniTask<SpinResult> Restore(SpinRequest request, IReadOnlyList<int> stops, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(_engine.Evaluate(request, stops));
        }
    }
}
