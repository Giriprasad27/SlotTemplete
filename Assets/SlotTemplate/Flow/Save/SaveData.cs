using System;
using System.Collections.Generic;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Flow.Save
{
    /// <summary>Where an unfinished round got to. Each stage is saved together with the money it moved.</summary>
    public enum RoundStage
    {
        /// <summary>Bet taken, no result yet. Recovery refunds the bet.</summary>
        Debited = 0,

        /// <summary>Result decided and saved, base win not paid. Recovery shows and pays it.</summary>
        ResultRecorded = 1,

        /// <summary>Base win paid. Recovery only finishes the features.</summary>
        BasePaid = 2,
    }

    /// <summary>Everything persisted between sessions. Plain fields so any serializer (JsonUtility, etc.) works.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public long balance;
        public int betLevel;
        public bool hasPendingRound;
        public RoundRecord round = new RoundRecord();

        public SaveData Clone()
        {
            return new SaveData
            {
                version = version,
                balance = balance,
                betLevel = betLevel,
                hasPendingRound = hasPendingRound,
                round = round.Clone(),
            };
        }
    }

    /// <summary>The unfinished round, if any.</summary>
    [Serializable]
    public sealed class RoundRecord
    {
        public RoundStage stage;
        public long lineBet;
        public int lineCount;
        public string reelSetId;
        public int[] stops = Array.Empty<int>();

        /// <summary>Total already paid out this round (base + features).</summary>
        public long roundWin;

        public string activeFeatureId = "";
        public string activeFeatureState = "";
        public List<string> completedFeatures = new List<string>();

        public long TotalBet => lineBet * lineCount;

        public SpinRequest ToRequest() => new SpinRequest(lineBet, lineCount, reelSetId);

        public RoundRecord Clone()
        {
            return new RoundRecord
            {
                stage = stage,
                lineBet = lineBet,
                lineCount = lineCount,
                reelSetId = reelSetId,
                stops = stops != null ? (int[])stops.Clone() : Array.Empty<int>(),
                roundWin = roundWin,
                activeFeatureId = activeFeatureId,
                activeFeatureState = activeFeatureState,
                completedFeatures = new List<string>(completedFeatures ?? new List<string>()),
            };
        }
    }
}
