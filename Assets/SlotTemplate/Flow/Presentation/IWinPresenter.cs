using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Spin;

namespace SlotTemplate.Flow.Presentation
{
    /// <summary>Implemented by whatever celebrates wins (highlights, lines, counters).</summary>
    public interface IWinPresenter
    {
        /// <summary>
        /// Presents the wins and completes once the player has seen them. Implementations may keep
        /// cycling win lines in the background until <see cref="Clear"/> is called.
        /// </summary>
        UniTask ShowWins(SpinResult result, SkipSignal skip, CancellationToken ct);

        void Clear();
    }
}
