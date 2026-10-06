using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Model;

namespace SlotTemplate.Flow.Presentation
{
    /// <summary>Implemented by the reels in the scene.</summary>
    public interface IReelPresenter
    {
        /// <summary>Spins and lands on <paramref name="grid"/>. Completes when every reel has settled.</summary>
        UniTask SpinTo(Grid grid, SkipSignal skip, CancellationToken ct);

        /// <summary>Shows <paramref name="grid"/> immediately, e.g. when resuming a saved round.</summary>
        void Show(Grid grid);
    }
}
