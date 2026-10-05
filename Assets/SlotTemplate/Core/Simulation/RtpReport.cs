using System.Collections.Generic;
using System.Text;

namespace SlotTemplate.Core.Simulation
{
    /// <summary>Results of an RTP simulation. Ratios are 0..1.</summary>
    public sealed class RtpReport
    {
        public long Spins { get; internal set; }
        public long TotalBet { get; internal set; }
        public long BaseWin { get; internal set; }
        public long FeatureWin { get; internal set; }
        public long WinningSpins { get; internal set; }
        public long MaxWin { get; internal set; }
        public Dictionary<string, long> FeatureTriggers { get; } = new Dictionary<string, long>();

        internal double SumOfSquares;

        public long TotalWin => BaseWin + FeatureWin;
        public double Rtp => TotalBet > 0 ? (double)TotalWin / TotalBet : 0;
        public double BaseRtp => TotalBet > 0 ? (double)BaseWin / TotalBet : 0;
        public double FeatureRtp => TotalBet > 0 ? (double)FeatureWin / TotalBet : 0;
        public double HitFrequency => Spins > 0 ? (double)WinningSpins / Spins : 0;

        /// <summary>Standard deviation of the per-spin return, in multiples of the bet. A volatility measure.</summary>
        public double StandardDeviation
        {
            get
            {
                if (Spins == 0) return 0;
                double mean = Rtp;
                double variance = SumOfSquares / Spins - mean * mean;
                return variance > 0 ? System.Math.Sqrt(variance) : 0;
            }
        }

        public double MaxWinMultiplier(long betPerSpin) => betPerSpin > 0 ? (double)MaxWin / betPerSpin : 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Spins:          {Spins:N0}");
            sb.AppendLine($"RTP:            {Rtp:P3}  (base {BaseRtp:P3}, features {FeatureRtp:P3})");
            sb.AppendLine($"Hit frequency:  {HitFrequency:P2}");
            sb.AppendLine($"Std deviation:  {StandardDeviation:F2}x bet");
            foreach (var pair in FeatureTriggers)
            {
                double every = pair.Value > 0 ? (double)Spins / pair.Value : 0;
                sb.AppendLine($"{pair.Key} triggers: {pair.Value:N0} (1 in {every:N0})");
            }
            return sb.ToString();
        }
    }
}
