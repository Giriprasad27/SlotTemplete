using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Flow.Presentation;

namespace SlotTemplate.Features.FreeSpins
{
    public interface IFreeSpinsPresenter
    {
        UniTask ShowIntro(int spinsAwarded, SkipSignal skip, CancellationToken ct);
        UniTask ShowRetrigger(int extraSpins, SkipSignal skip, CancellationToken ct);
        void SetProgress(int spinNumber, int totalSpins, long featureWin);
        UniTask ShowOutro(long featureWin, SkipSignal skip, CancellationToken ct);
        void Hide();
    }
}
