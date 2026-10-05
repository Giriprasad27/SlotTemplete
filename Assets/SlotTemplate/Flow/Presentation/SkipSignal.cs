using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SlotTemplate.Flow.Presentation
{
    /// <summary>
    /// "Hurry up" request from the player (quick stop, tap to skip). Presenters jump to their end state
    /// when it is raised. Unlike cancellation it never aborts the round, so money and save stay consistent.
    /// </summary>
    public sealed class SkipSignal
    {
        public bool IsRequested { get; private set; }

        public event Action Requested;

        public void Request()
        {
            if (IsRequested) return;
            IsRequested = true;
            Requested?.Invoke();
        }

        public void Reset() => IsRequested = false;

        /// <summary>Waits for <paramref name="seconds"/>, returning early if a skip is requested.</summary>
        public async UniTask WaitAsync(float seconds, CancellationToken ct)
        {
            if (IsRequested || seconds <= 0f) return;

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                void OnRequested() => linked.Cancel();
                Requested += OnRequested;
                try
                {
#if UNITY_5_3_OR_NEWER
                    await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: linked.Token).SuppressCancellationThrow();
#else
                    // Outside Unity (plain .NET test runs) there is no player loop to drive UniTask.Delay.
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(seconds), linked.Token).AsUniTask().SuppressCancellationThrow();
#endif
                }
                finally
                {
                    Requested -= OnRequested;
                }
            }

            ct.ThrowIfCancellationRequested();
        }
    }
}
