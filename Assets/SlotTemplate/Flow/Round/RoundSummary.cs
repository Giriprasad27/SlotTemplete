namespace SlotTemplate.Flow.Round
{
    public readonly struct RoundSummary
    {
        public static readonly RoundSummary NotPlayed = default;

        public readonly bool Played;
        public readonly long TotalBet;
        public readonly long TotalWin;
        public readonly bool FeatureTriggered;
        public readonly bool Resumed;

        public RoundSummary(long totalBet, long totalWin, bool featureTriggered, bool resumed)
        {
            Played = true;
            TotalBet = totalBet;
            TotalWin = totalWin;
            FeatureTriggered = featureTriggered;
            Resumed = resumed;
        }
    }
}
