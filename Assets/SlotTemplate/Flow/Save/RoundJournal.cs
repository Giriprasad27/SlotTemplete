using System;
using System.Collections.Generic;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Economy;

namespace SlotTemplate.Flow.Save
{
    /// <summary>
    /// The only place that moves money during a round. Every method changes the wallet and writes the
    /// save in one step, so a crash can never take a bet without recording it, or pay a win twice.
    /// </summary>
    public sealed class RoundJournal
    {
        private readonly ISaveStore _store;
        private readonly SaveData _data;

        public Wallet Wallet { get; }
        public int SavedBetLevel => _data.betLevel;
        public bool HasPendingRound => _data.hasPendingRound;

        /// <summary>The unfinished round. Only meaningful while <see cref="HasPendingRound"/> is true.</summary>
        public RoundRecord Pending => _data.round;

        public long RoundWin => _data.hasPendingRound ? _data.round.roundWin : 0;

        public RoundJournal(ISaveStore store, long startingBalance, int defaultBetLevel = 0)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _data = store.Load();
            if (_data == null)
            {
                _data = new SaveData { balance = startingBalance, betLevel = defaultBetLevel };
                _store.Save(_data);
            }

            Wallet = new Wallet(_data.balance);
        }

        /// <summary>Takes the bet and opens the round. Returns false (and changes nothing) if the player can't afford it.</summary>
        public bool BeginRound(SpinRequest request)
        {
            if (HasPendingRound) throw new InvalidOperationException("A round is already in progress.");
            if (!Wallet.TryDebit(request.TotalBet)) return false;

            _data.hasPendingRound = true;
            _data.round = new RoundRecord
            {
                stage = RoundStage.Debited,
                lineBet = request.LineBet,
                lineCount = request.LineCount,
                reelSetId = request.ReelSetId,
            };
            Commit();
            return true;
        }

        /// <summary>Returns the bet of a round that never got a result (e.g. the server failed).</summary>
        public void Refund()
        {
            RequirePending(RoundStage.Debited);
            Wallet.Credit(_data.round.TotalBet);
            ClearRound();
        }

        public void RecordResult(IReadOnlyList<int> stops)
        {
            RequirePending(RoundStage.Debited);
            var copy = new int[stops.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = stops[i];

            _data.round.stops = copy;
            _data.round.stage = RoundStage.ResultRecorded;
            Commit();
        }

        public void PayBase(long amount)
        {
            RequirePending(RoundStage.ResultRecorded);
            Wallet.Credit(amount);
            _data.round.roundWin += amount;
            _data.round.stage = RoundStage.BasePaid;
            Commit();
        }

        /// <summary>Pays part of a feature's win and saves the feature's progress in the same write.</summary>
        public void PayFeature(string featureId, long amount, string featureState)
        {
            RequirePending(RoundStage.BasePaid);
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            Wallet.Credit(amount);
            _data.round.roundWin += amount;
            _data.round.activeFeatureId = featureId;
            _data.round.activeFeatureState = featureState ?? "";
            Commit();
        }

        public void SaveFeatureState(string featureId, string featureState) => PayFeature(featureId, 0, featureState);

        /// <summary>Saved progress of <paramref name="featureId"/>, or null if it has not started.</summary>
        public string GetFeatureState(string featureId) =>
            HasPendingRound && _data.round.activeFeatureId == featureId && !string.IsNullOrEmpty(_data.round.activeFeatureState)
                ? _data.round.activeFeatureState
                : null;

        public bool IsFeatureCompleted(string featureId) => HasPendingRound && _data.round.completedFeatures.Contains(featureId);

        public void CompleteFeature(string featureId)
        {
            RequirePending(RoundStage.BasePaid);
            _data.round.completedFeatures.Add(featureId);
            _data.round.activeFeatureId = "";
            _data.round.activeFeatureState = "";
            Commit();
        }

        public void CompleteRound()
        {
            RequirePending(RoundStage.BasePaid);
            ClearRound();
        }

        public void SaveBetLevel(int level)
        {
            _data.betLevel = level;
            Commit();
        }

        private void ClearRound()
        {
            _data.hasPendingRound = false;
            _data.round = new RoundRecord();
            Commit();
        }

        private void Commit()
        {
            _data.balance = Wallet.Balance;
            _store.Save(_data);
        }

        private void RequirePending(RoundStage stage)
        {
            if (!HasPendingRound) throw new InvalidOperationException("No round in progress.");
            if (_data.round.stage != stage)
                throw new InvalidOperationException($"Round is at stage {_data.round.stage}; expected {stage}.");
        }
    }
}
