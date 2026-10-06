using System;
using System.Collections.Generic;
using SlotTemplate.Core.Random;
using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Core.Simulation
{
    /// <summary>
    /// Plays millions of spins with math only to measure RTP, hit rate and volatility.
    /// Call <see cref="Run"/> repeatedly in chunks to show progress.
    /// </summary>
    public sealed class RtpSimulator
    {
        private readonly SpinEngine _engine;
        private readonly SpinRequest _request;
        private readonly List<IFeatureSimulation> _features = new List<IFeatureSimulation>();

        public RtpReport Report { get; } = new RtpReport();

        public RtpSimulator(SlotMath math, IReadOnlyList<ISpinRule> rules, SpinRequest request, int seed)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            _request = request;
            _engine = new SpinEngine(math, new SeededRandom(seed), rules);
            foreach (var rule in rules)
            {
                if (rule is IFeatureSimulation feature)
                {
                    _features.Add(feature);
                    Report.FeatureTriggers[feature.FeatureId] = 0;
                }
            }
        }

        public void Run(long spins)
        {
            long bet = _request.TotalBet;
            for (long i = 0; i < spins; i++)
            {
                var result = _engine.Spin(_request);
                long spinWin = result.TotalWin;
                Report.BaseWin += result.TotalWin;

                foreach (var feature in _features)
                {
                    if (!feature.IsTriggered(result)) continue;

                    Report.FeatureTriggers[feature.FeatureId]++;
                    long featureWin = feature.Simulate(result, _engine);
                    Report.FeatureWin += featureWin;
                    spinWin += featureWin;
                }

                Report.Spins++;
                Report.TotalBet += bet;
                if (spinWin > 0) Report.WinningSpins++;
                if (spinWin > Report.MaxWin) Report.MaxWin = spinWin;

                double ratio = (double)spinWin / bet;
                Report.SumOfSquares += ratio * ratio;
            }
        }
    }
}
