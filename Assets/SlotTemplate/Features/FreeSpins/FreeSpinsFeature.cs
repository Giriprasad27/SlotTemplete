using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Features;

namespace SlotTemplate.Features.FreeSpins
{
    /// <summary>
    /// Playable half of free spins. Each spin is paid and its progress saved in one write, so a crash
    /// resumes with the right number of spins left and never pays a spin twice.
    /// </summary>
    public sealed class FreeSpinsFeature : ISlotFeature
    {
        private readonly FreeSpinsSettings _settings;
        private readonly IFreeSpinsPresenter _presenter;

        public string Id => FreeSpinsRule.Id;

        public FreeSpinsFeature(FreeSpinsSettings settings, IFreeSpinsPresenter presenter)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _presenter = presenter;
        }

        public bool ShouldPlay(SpinResult result) => result.Has<FreeSpinsResult>();

        public async UniTask Play(RoundContext context, CancellationToken ct)
        {
            var progress = Progress.Parse(context.SavedState);
            if (progress == null)
            {
                progress = new Progress { Remaining = context.Trigger.Get<FreeSpinsResult>().SpinsAwarded };
                context.SaveState(progress.ToString());
                if (_presenter != null) await _presenter.ShowIntro(progress.Remaining, context.Skip, ct);
            }

            var request = context.BaseRequest.ForFeature(_settings.reelSetId);
            while (progress.Remaining > 0 && progress.Played < _settings.maxTotalSpins)
            {
                _presenter?.SetProgress(progress.Played + 1, progress.Played + progress.Remaining, progress.Win);

                var spin = await context.SpinAsync(request, ct);
                progress.Remaining--;
                progress.Played++;
                progress.Win += spin.TotalWin;

                int extra = spin.TryGet<FreeSpinsResult>(out var retrigger) ? retrigger.SpinsAwarded : 0;
                progress.Remaining += extra;

                context.Pay(spin.TotalWin, progress.ToString());
                _presenter?.SetProgress(progress.Played, progress.Played + progress.Remaining, progress.Win);

                if (extra > 0 && _presenter != null) await _presenter.ShowRetrigger(extra, context.Skip, ct);
            }

            if (_presenter != null)
            {
                await _presenter.ShowOutro(progress.Win, context.Skip, ct);
                _presenter.Hide();
            }
        }

        /// <summary>Saved as "remaining|played|win".</summary>
        private sealed class Progress
        {
            public int Remaining;
            public int Played;
            public long Win;

            public static Progress Parse(string state)
            {
                if (string.IsNullOrEmpty(state)) return null;

                var parts = state.Split('|');
                if (parts.Length != 3) return null;

                return new Progress
                {
                    Remaining = int.Parse(parts[0], CultureInfo.InvariantCulture),
                    Played = int.Parse(parts[1], CultureInfo.InvariantCulture),
                    Win = long.Parse(parts[2], CultureInfo.InvariantCulture),
                };
            }

            public override string ToString() =>
                string.Join("|", Remaining.ToString(CultureInfo.InvariantCulture), Played.ToString(CultureInfo.InvariantCulture), Win.ToString(CultureInfo.InvariantCulture));
        }
    }
}
