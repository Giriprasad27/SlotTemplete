using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Flow.Features
{
    /// <summary>
    /// The playable half of a feature (free spins, bonus, jackpot). Its math half is an
    /// <see cref="ISpinRule"/> that writes a typed result the feature then reads.
    /// </summary>
    public interface ISlotFeature
    {
        /// <summary>Stable id; used in the save to resume this feature after a crash.</summary>
        string Id { get; }

        bool ShouldPlay(SpinResult result);

        /// <summary>Plays the feature. Pay winnings through <see cref="RoundContext.Pay"/> so they are saved.</summary>
        UniTask Play(RoundContext context, CancellationToken ct);
    }
}
