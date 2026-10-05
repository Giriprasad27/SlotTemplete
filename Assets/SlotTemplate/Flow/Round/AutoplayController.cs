using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Flow.Economy;

namespace SlotTemplate.Flow.Round
{
    [Serializable]
    public sealed class AutoplaySettings
    {
        public int spins = 25;
        public bool stopOnFeature = true;

        /// <summary>Stop after a round that wins at least this many times the bet. 0 disables.</summary>
        public long stopOnWinMultiplier = 50;

        /// <summary>Stop once the balance has dropped this many credits since autoplay started. 0 disables.</summary>
        public long lossLimit;
    }

    /// <summary>Autoplay is just a loop around <see cref="RoundRunner.PlayRound"/> with stop conditions.</summary>
    public sealed class AutoplayController
    {
        private readonly RoundRunner _runner;
        private readonly Wallet _wallet;
        private bool _stopRequested;

        public bool IsRunning { get; private set; }
        public event Action<bool> RunningChanged;

        public AutoplayController(RoundRunner runner, Wallet wallet)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public void Stop() => _stopRequested = true;

        public async UniTask Run(AutoplaySettings settings, CancellationToken ct)
        {
            if (IsRunning) return;

            IsRunning = true;
            _stopRequested = false;
            RunningChanged?.Invoke(true);
            long startBalance = _wallet.Balance;

            try
            {
                for (int i = 0; i < settings.spins && !_stopRequested && !ct.IsCancellationRequested; i++)
                {
                    if (!_runner.CanStartRound) break;

                    var round = await _runner.PlayRound(ct);
                    if (!round.Played) break;
                    if (settings.stopOnFeature && round.FeatureTriggered) break;
                    if (settings.stopOnWinMultiplier > 0 && round.TotalWin >= round.TotalBet * settings.stopOnWinMultiplier) break;
                    if (settings.lossLimit > 0 && startBalance - _wallet.Balance >= settings.lossLimit) break;
                }
            }
            finally
            {
                IsRunning = false;
                RunningChanged?.Invoke(false);
            }
        }
    }
}
