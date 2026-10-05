using System;
using SlotTemplate.Core;
using SlotTemplate.Core.Economy;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Random;
using SlotTemplate.Data;
using SlotTemplate.Presentation.Reels;
using SlotTemplate.Presentation.Wins;
using SlotTemplate.UI;
using UnityEngine;

namespace SlotTemplate.Game
{
    /// <summary>
    /// Composition root and flow controller. Builds the logic from config, then connects it to the
    /// reels, win presentation and HUD. This is the only class that knows about every layer.
    /// </summary>
    public sealed class SlotGameController : MonoBehaviour
    {
        [SerializeField] private SlotMachineConfig config;
        [SerializeField] private ReelsPresenter reels;
        [SerializeField] private WinPresenter wins;
        [SerializeField] private SlotHud hud;

        private SlotMachine _machine;
        private SpinOutcome _pendingOutcome;

        public GameState State { get; private set; } = GameState.Idle;
        public SlotMachine Machine => _machine;

        /// <summary>Raised when the bet is taken and the reels start. Hook spin sounds or analytics here.</summary>
        public event Action<SpinOutcome> SpinStarted;

        /// <summary>Raised when the reels have landed and winnings are credited.</summary>
        public event Action<SpinOutcome> SpinCompleted;

        private void Awake()
        {
            var rules = SlotRulesFactory.Build(config);
            IRandomNumberGenerator rng = config.UseFixedSeed
                ? new SystemRandomNumberGenerator(config.Seed)
                : new SystemRandomNumberGenerator();

            _machine = new SlotMachine(
                rules,
                new Wallet(config.StartingBalance),
                new BetModel(config.LineBetLevels, rules.Paylines.Count, config.DefaultBetLevel),
                rng);

            reels.Initialize(rules, config.Symbols);
        }

        private void OnEnable()
        {
            if (_machine == null) return;

            if (hud != null)
            {
                hud.SpinPressed += OnSpinPressed;
                hud.BetUpPressed += OnBetUpPressed;
                hud.BetDownPressed += OnBetDownPressed;
            }

            _machine.Wallet.BalanceChanged += OnBalanceChanged;
            _machine.Bet.BetChanged += OnBetChanged;
        }

        private void OnDisable()
        {
            if (_machine == null) return;

            if (hud != null)
            {
                hud.SpinPressed -= OnSpinPressed;
                hud.BetUpPressed -= OnBetUpPressed;
                hud.BetDownPressed -= OnBetDownPressed;
            }

            _machine.Wallet.BalanceChanged -= OnBalanceChanged;
            _machine.Bet.BetChanged -= OnBetChanged;
        }

        private void Start()
        {
            OnBalanceChanged(_machine.Wallet.Balance);
            OnBetChanged(_machine.Bet.TotalBet);
            if (hud != null)
            {
                hud.SetWin(0);
                hud.SetSpinning(false);
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) OnSpinPressed();
        }
#endif

        /// <summary>Spin button behaviour: start a spin when idle, slam-stop when spinning.</summary>
        public void OnSpinPressed()
        {
            if (State == GameState.Spinning)
            {
                reels.QuickStop();
                return;
            }

            if (!_machine.TrySpin(out var outcome))
            {
                if (hud != null) hud.ShowMessage("Not enough credits");
                return;
            }

            State = GameState.Spinning;
            _pendingOutcome = outcome;

            wins.Clear();
            if (hud != null)
            {
                hud.SetWin(0);
                hud.SetSpinning(true);
                hud.SetBetButtons(false, false);
            }

            SpinStarted?.Invoke(outcome);
            reels.Spin(outcome.Grid, OnReelsStopped);
        }

        private void OnReelsStopped()
        {
            var outcome = _pendingOutcome;
            _pendingOutcome = null;

            _machine.CollectWin();
            State = GameState.Idle;

            if (hud != null)
            {
                hud.SetWin(outcome.TotalWin);
                hud.SetSpinning(false);
                hud.SetBetButtons(_machine.Bet.CanDecrease, _machine.Bet.CanIncrease);
            }

            wins.Show(outcome);
            SpinCompleted?.Invoke(outcome);
        }

        private void OnBetUpPressed()
        {
            if (State == GameState.Idle) _machine.Bet.Increase();
        }

        private void OnBetDownPressed()
        {
            if (State == GameState.Idle) _machine.Bet.Decrease();
        }

        private void OnBalanceChanged(long balance)
        {
            if (hud != null) hud.SetBalance(balance);
        }

        private void OnBetChanged(long totalBet)
        {
            if (hud == null) return;
            hud.SetBet(totalBet);
            hud.SetBetButtons(_machine.Bet.CanDecrease, _machine.Bet.CanIncrease);
        }
    }
}
