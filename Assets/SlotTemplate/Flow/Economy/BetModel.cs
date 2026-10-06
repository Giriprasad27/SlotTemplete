using System;
using System.Collections.Generic;

namespace SlotTemplate.Flow.Economy
{
    /// <summary>Selectable line-bet levels. Total bet is the line bet times the number of paylines.</summary>
    public sealed class BetModel
    {
        private readonly long[] _lineBetLevels;

        public int LineCount { get; }
        public int LevelIndex { get; private set; }
        public IReadOnlyList<long> LineBetLevels => _lineBetLevels;

        public long LineBet => _lineBetLevels[LevelIndex];
        public long TotalBet => LineBet * LineCount;
        public bool CanIncrease => LevelIndex < _lineBetLevels.Length - 1;
        public bool CanDecrease => LevelIndex > 0;

        /// <summary>Raised with the new total bet whenever the level changes.</summary>
        public event Action<long> BetChanged;

        public BetModel(IReadOnlyList<long> lineBetLevels, int lineCount, int startingLevel = 0)
        {
            if (lineBetLevels == null || lineBetLevels.Count == 0)
                throw new ArgumentException("At least one bet level is required.", nameof(lineBetLevels));
            if (lineCount <= 0) throw new ArgumentOutOfRangeException(nameof(lineCount));

            _lineBetLevels = new long[lineBetLevels.Count];
            for (int i = 0; i < lineBetLevels.Count; i++)
            {
                if (lineBetLevels[i] <= 0) throw new ArgumentException("Bet levels must be positive.", nameof(lineBetLevels));
                _lineBetLevels[i] = lineBetLevels[i];
            }

            LineCount = lineCount;
            LevelIndex = Clamp(startingLevel);
        }

        public void Increase() => SetLevel(LevelIndex + 1);
        public void Decrease() => SetLevel(LevelIndex - 1);

        public void SetLevel(int index)
        {
            int clamped = Clamp(index);
            if (clamped == LevelIndex) return;

            LevelIndex = clamped;
            BetChanged?.Invoke(TotalBet);
        }

        private int Clamp(int index) => Math.Max(0, Math.Min(index, _lineBetLevels.Length - 1));
    }
}
