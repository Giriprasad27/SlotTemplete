using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Economy;
using SlotTemplate.Flow.Features;
using SlotTemplate.Flow.Outcome;
using SlotTemplate.Flow.Presentation;
using SlotTemplate.Flow.Save;
using SlotTemplate.Flow.State;

namespace SlotTemplate.Flow.Round
{
    /// <summary>
    /// Plays rounds. <see cref="PlayRound"/> reads top to bottom: take the bet, get the result, show it,
    /// pay, play features, finish. Every money step goes through <see cref="RoundJournal"/>, so an
    /// interrupted round is finished on the next launch by <see cref="BootAsync"/>.
    /// </summary>
    public sealed class RoundRunner
    {
        private readonly SlotGame _game;
        private readonly IOutcomeProvider _outcome;
        private readonly RoundJournal _journal;
        private readonly BetModel _bet;
        private readonly IReelPresenter _reels;
        private readonly IWinPresenter _wins;
        private readonly IReadOnlyList<ISlotFeature> _features;
        private readonly string _baseReelSetId;

        /// <summary>Raise to make presenters jump to the end of what they are showing (quick stop).</summary>
        public SkipSignal Skip { get; } = new SkipSignal();

        public long CurrentRoundWin => _journal.RoundWin;

        public bool CanStartRound =>
            _game.Current == GameState.Idle && (_journal.HasPendingRound || _journal.Wallet.CanAfford(_bet.TotalBet));

        /// <summary>Raised with the round's running total whenever a win is paid.</summary>
        public event Action<long> RoundWinChanged;

        /// <summary>Raised after a round (including its features) has fully finished.</summary>
        public event Action<RoundSummary> RoundCompleted;

        /// <summary>Short messages for the player, e.g. "Not enough credits".</summary>
        public event Action<string> Notice;

        public RoundRunner(
            SlotGame game,
            IOutcomeProvider outcome,
            RoundJournal journal,
            BetModel bet,
            IReelPresenter reels,
            IWinPresenter wins,
            IReadOnlyList<ISlotFeature> features,
            string baseReelSetId = SpinRequest.BaseReelSet)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _bet = bet ?? throw new ArgumentNullException(nameof(bet));
            _reels = reels ?? throw new ArgumentNullException(nameof(reels));
            _wins = wins ?? throw new ArgumentNullException(nameof(wins));
            _features = features ?? Array.Empty<ISlotFeature>();
            _baseReelSetId = baseReelSetId;
        }

        /// <summary>Leaves Boot: finishes a round left over from a crash, then goes Idle.</summary>
        public async UniTask BootAsync(CancellationToken ct)
        {
            if (_game.Current != GameState.Boot) throw new InvalidOperationException("Already booted.");

            if (!_journal.HasPendingRound)
            {
                _game.Enter(GameState.Idle);
                return;
            }

            _game.Enter(GameState.InRound);
            try
            {
                await ResumePending(ct);
            }
            finally
            {
                ReturnToIdle();
            }
        }

        public async UniTask<RoundSummary> PlayRound(CancellationToken ct)
        {
            if (_game.Current != GameState.Idle) return RoundSummary.NotPlayed;
            _game.Enter(GameState.InRound); // before any await, so a second tap can't start another round

            try
            {
                // A round that failed part way (e.g. a presenter threw) is finished before a new one starts.
                if (_journal.HasPendingRound) return await ResumePending(ct);

                var request = new SpinRequest(_bet.LineBet, _bet.LineCount, _baseReelSetId);
                if (!_journal.BeginRound(request))
                {
                    Notice?.Invoke("Not enough credits");
                    return RoundSummary.NotPlayed;
                }

                Skip.Reset();
                _wins.Clear();
                RoundWinChanged?.Invoke(0);

                SpinResult result;
                try
                {
                    result = await _outcome.Spin(request, ct);
                }
                catch
                {
                    _journal.Refund(); // no result means no round: give the bet back
                    throw;
                }

                _journal.RecordResult(result.Grid.Stops);
                return await FinishRound(result, resumed: false, ct);
            }
            finally
            {
                ReturnToIdle(); // always unlock input, even if something threw
            }
        }

        private async UniTask<RoundSummary> ResumePending(CancellationToken ct)
        {
            var record = _journal.Pending;
            if (record.stage == RoundStage.Debited)
            {
                _journal.Refund();
                Notice?.Invoke("Unfinished spin refunded");
                return RoundSummary.NotPlayed;
            }

            var result = await _outcome.Restore(record.ToRequest(), record.stops, ct);
            Skip.Reset();
            return await FinishRound(result, resumed: true, ct);
        }

        private async UniTask<RoundSummary> FinishRound(SpinResult result, bool resumed, CancellationToken ct)
        {
            if (_journal.Pending.stage == RoundStage.ResultRecorded)
            {
                await _reels.SpinTo(result.Grid, Skip, ct);
                await _wins.ShowWins(result, Skip, ct);
                _journal.PayBase(result.TotalWin);
                RoundWinChanged?.Invoke(_journal.RoundWin);
            }
            else
            {
                _reels.Show(result.Grid);
                RoundWinChanged?.Invoke(_journal.RoundWin);
            }

            bool featureTriggered = false;
            foreach (var feature in _features)
            {
                if (!feature.ShouldPlay(result) || _journal.IsFeatureCompleted(feature.Id)) continue;

                if (!featureTriggered)
                {
                    featureTriggered = true;
                    _game.Enter(GameState.InFeature);
                }

                Skip.Reset();
                _wins.Clear();
                var context = new RoundContext(this, feature.Id, result, _journal.GetFeatureState(feature.Id), Skip);
                await feature.Play(context, ct);
                _journal.CompleteFeature(feature.Id);
            }

            long totalWin = _journal.RoundWin;
            long totalBet = _journal.Pending.TotalBet;
            _journal.CompleteRound();

            var summary = new RoundSummary(totalBet, totalWin, featureTriggered, resumed);
            RoundCompleted?.Invoke(summary);
            return summary;
        }

        internal async UniTask<SpinResult> PlayFeatureSpin(SpinRequest request, CancellationToken ct)
        {
            Skip.Reset();
            _wins.Clear();
            var result = await _outcome.Spin(request, ct);
            await _reels.SpinTo(result.Grid, Skip, ct);
            await _wins.ShowWins(result, Skip, ct);
            return result;
        }

        internal void PayFeature(string featureId, long amount, string state)
        {
            _journal.PayFeature(featureId, amount, state);
            if (amount > 0) RoundWinChanged?.Invoke(_journal.RoundWin);
        }

        private void ReturnToIdle()
        {
            if (_game.Current == GameState.InRound || _game.Current == GameState.InFeature)
                _game.Enter(GameState.Idle);
        }
    }
}
