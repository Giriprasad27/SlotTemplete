using System;
using SlotTemplate.Core.Economy;
using SlotTemplate.Core.Evaluation;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Rules;

namespace SlotTemplate.Core
{
    /// <summary>
    /// Game-logic facade: takes the bet, spins, evaluates and pays out.
    /// Knows nothing about Unity, so it can be unit tested or moved to a server.
    /// </summary>
    public sealed class SlotMachine
    {
        private readonly SpinEngine _spinEngine;
        private readonly IWinEvaluator _evaluator;

        public SlotRules Rules { get; }
        public Wallet Wallet { get; }
        public BetModel Bet { get; }
        public SpinOutcome LastOutcome { get; private set; }

        public SlotMachine(SlotRules rules, Wallet wallet, BetModel bet, IRandomNumberGenerator rng, IWinEvaluator evaluator = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Bet = bet ?? throw new ArgumentNullException(nameof(bet));
            _spinEngine = new SpinEngine(rules, rng ?? throw new ArgumentNullException(nameof(rng)));
            _evaluator = evaluator ?? new PaylineWinEvaluator(rules);
        }

        public bool CanSpin => Wallet.CanAfford(Bet.TotalBet);

        /// <summary>
        /// Debits the bet, spins and evaluates. Winnings are not credited yet so the presentation can
        /// reveal them first; call <see cref="CollectWin"/> once they have been shown.
        /// </summary>
        public bool TrySpin(out SpinOutcome outcome)
        {
            outcome = null;
            if (!Wallet.TryDebit(Bet.TotalBet)) return false;

            var grid = _spinEngine.Spin();
            outcome = _evaluator.Evaluate(grid, Bet.LineBet, Bet.TotalBet);
            LastOutcome = outcome;
            return true;
        }

        /// <summary>Credits the last outcome's winnings. Safe to call more than once per spin.</summary>
        public void CollectWin()
        {
            if (LastOutcome == null) return;

            Wallet.Credit(LastOutcome.TotalWin);
            LastOutcome = null;
        }
    }
}
