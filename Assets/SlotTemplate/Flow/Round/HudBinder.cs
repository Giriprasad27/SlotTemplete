using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Flow.Economy;
using SlotTemplate.Flow.Presentation;
using SlotTemplate.Flow.Save;
using SlotTemplate.Flow.State;

namespace SlotTemplate.Flow.Round
{
    /// <summary>Connects the HUD's buttons to the game, and the game's events to the HUD's readouts.</summary>
    public sealed class HudBinder : IDisposable
    {
        private readonly IHud _hud;
        private readonly SlotGame _game;
        private readonly RoundRunner _runner;
        private readonly AutoplayController _autoplay;
        private readonly AutoplaySettings _autoplaySettings;
        private readonly Wallet _wallet;
        private readonly BetModel _bet;
        private readonly RoundJournal _journal;
        private readonly CancellationToken _lifetime;

        public HudBinder(IHud hud, SlotGame game, RoundRunner runner, AutoplayController autoplay, AutoplaySettings autoplaySettings,
            RoundJournal journal, BetModel bet, CancellationToken lifetime)
        {
            _hud = hud;
            _game = game;
            _runner = runner;
            _autoplay = autoplay;
            _autoplaySettings = autoplaySettings;
            _journal = journal;
            _wallet = journal.Wallet;
            _bet = bet;
            _lifetime = lifetime;

            _hud.SpinPressed += OnSpinPressed;
            _hud.BetUpPressed += OnBetUp;
            _hud.BetDownPressed += OnBetDown;
            _hud.AutoplayPressed += OnAutoplayPressed;
            _wallet.BalanceChanged += _hud.SetBalance;
            _bet.BetChanged += OnBetChanged;
            _game.StateChanged += OnStateChanged;
            _runner.RoundWinChanged += _hud.SetWin;
            _runner.Notice += _hud.ShowMessage;
            _autoplay.RunningChanged += OnAutoplayChanged;

            _hud.SetBalance(_wallet.Balance);
            _hud.SetBet(_bet.TotalBet);
            _hud.SetWin(0);
            _hud.SetAutoplay(false);
            RefreshState();
        }

        public void Dispose()
        {
            _hud.SpinPressed -= OnSpinPressed;
            _hud.BetUpPressed -= OnBetUp;
            _hud.BetDownPressed -= OnBetDown;
            _hud.AutoplayPressed -= OnAutoplayPressed;
            _wallet.BalanceChanged -= _hud.SetBalance;
            _bet.BetChanged -= OnBetChanged;
            _game.StateChanged -= OnStateChanged;
            _runner.RoundWinChanged -= _hud.SetWin;
            _runner.Notice -= _hud.ShowMessage;
            _autoplay.RunningChanged -= OnAutoplayChanged;
        }

        private void OnSpinPressed()
        {
            if (_autoplay.IsRunning)
            {
                _autoplay.Stop();
                _runner.Skip.Request();
                return;
            }

            if (_game.Current == GameState.Idle) _runner.PlayRound(_lifetime).Forget();
            else _runner.Skip.Request(); // quick stop
        }

        private void OnAutoplayPressed()
        {
            if (_autoplay.IsRunning) _autoplay.Stop();
            else if (_game.Current == GameState.Idle) _autoplay.Run(_autoplaySettings, _lifetime).Forget();
        }

        private void OnBetUp()
        {
            if (CanChangeBet) _bet.Increase();
        }

        private void OnBetDown()
        {
            if (CanChangeBet) _bet.Decrease();
        }

        private void OnBetChanged(long totalBet)
        {
            _journal.SaveBetLevel(_bet.LevelIndex);
            _hud.SetBet(totalBet);
            RefreshState();
        }

        private void OnStateChanged(GameState from, GameState to) => RefreshState();
        private void OnAutoplayChanged(bool running)
        {
            _hud.SetAutoplay(running);
            RefreshState();
        }

        private bool CanChangeBet => _game.Current == GameState.Idle && !_autoplay.IsRunning;

        private void RefreshState() => _hud.SetState(_game.Current, CanChangeBet);
    }
}
