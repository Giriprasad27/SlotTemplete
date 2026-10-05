using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Outcome;
using SlotTemplate.Flow.Presentation;

namespace SlotTemplate.Tests
{
    // Fakes complete synchronously, so tests can run rounds without Unity's player loop.

    internal sealed class FakeReels : IReelPresenter
    {
        public int SpinCount;
        public int ShowCount;
        public Exception ThrowOnSpin;

        public UniTask SpinTo(Grid grid, SkipSignal skip, CancellationToken ct)
        {
            SpinCount++;
            if (ThrowOnSpin != null)
            {
                var error = ThrowOnSpin;
                ThrowOnSpin = null;
                throw error;
            }
            return UniTask.CompletedTask;
        }

        public void Show(Grid grid) => ShowCount++;
    }

    internal sealed class FakeWins : IWinPresenter
    {
        public int ShowCount;
        public UniTask ShowWins(SpinResult result, SkipSignal skip, CancellationToken ct)
        {
            ShowCount++;
            return UniTask.CompletedTask;
        }

        public void Clear() { }
    }

    /// <summary>Uses a real engine but lets tests queue exact stops or a failure.</summary>
    internal sealed class ScriptedOutcome : IOutcomeProvider
    {
        private readonly SpinEngine _engine;
        private readonly Queue<int[]> _stops = new Queue<int[]>();

        public Exception FailNext;
        public int SpinCalls;

        public ScriptedOutcome(SpinEngine engine) => _engine = engine;

        public void Queue(params int[] stops) => _stops.Enqueue(stops);

        public UniTask<SpinResult> Spin(SpinRequest request, CancellationToken ct)
        {
            SpinCalls++;
            if (FailNext != null)
            {
                var error = FailNext;
                FailNext = null;
                return UniTask.FromException<SpinResult>(error);
            }

            var stops = _stops.Count > 0 ? _stops.Dequeue() : new int[_engine.Math.ReelCount];
            return UniTask.FromResult(_engine.Evaluate(request, stops));
        }

        public UniTask<SpinResult> Restore(SpinRequest request, IReadOnlyList<int> stops, CancellationToken ct) =>
            UniTask.FromResult(_engine.Evaluate(request, stops));
    }

    internal static class UniTaskTestExtensions
    {
        /// <summary>Gets the result of a task that has already completed synchronously.</summary>
        public static T Result<T>(this UniTask<T> task) => task.GetAwaiter().GetResult();

        public static void Wait(this UniTask task) => task.GetAwaiter().GetResult();
    }
}
