using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Flow.Presentation;
using TMPro;
using UnityEngine;

namespace SlotTemplate.Features.FreeSpins
{
    /// <summary>Banner and counter for free spins. Lives on the feature's presenter prefab.</summary>
    public sealed class FreeSpinsPresenter : MonoBehaviour, IFreeSpinsPresenter
    {
        [SerializeField] private GameObject banner;
        [SerializeField] private TMP_Text bannerLabel;
        [SerializeField] private GameObject counter;
        [SerializeField] private TMP_Text counterLabel;
        [SerializeField] private float introSeconds = 1.5f;
        [SerializeField] private float retriggerSeconds = 1.2f;
        [SerializeField] private float outroSeconds = 2f;

        public async UniTask ShowIntro(int spinsAwarded, SkipSignal skip, CancellationToken ct)
        {
            gameObject.SetActive(true);
            SetCounterVisible(true);
            await ShowBanner($"FREE SPINS\n{spinsAwarded}", introSeconds, skip, ct);
        }

        public UniTask ShowRetrigger(int extraSpins, SkipSignal skip, CancellationToken ct) =>
            ShowBanner($"+{extraSpins} FREE SPINS", retriggerSeconds, skip, ct);

        public void SetProgress(int spinNumber, int totalSpins, long featureWin)
        {
            gameObject.SetActive(true);
            SetCounterVisible(true);
            if (counterLabel != null) counterLabel.text = $"FREE SPIN {spinNumber} / {totalSpins}\nWIN {featureWin:N0}";
        }

        public async UniTask ShowOutro(long featureWin, SkipSignal skip, CancellationToken ct)
        {
            SetCounterVisible(false);
            await ShowBanner($"FREE SPINS WIN\n{featureWin:N0}", outroSeconds, skip, ct);
        }

        public void Hide()
        {
            if (banner != null) banner.SetActive(false);
            SetCounterVisible(false);
            gameObject.SetActive(false);
        }

        private async UniTask ShowBanner(string text, float seconds, SkipSignal skip, CancellationToken ct)
        {
            if (bannerLabel != null) bannerLabel.text = text;
            if (banner != null) banner.SetActive(true);

            // A skip from the reels shouldn't also skip the banner, so wait on a fresh signal reset.
            skip.Reset();
            await skip.WaitAsync(seconds, ct);

            if (banner != null) banner.SetActive(false);
        }

        private void SetCounterVisible(bool visible)
        {
            if (counter != null) counter.SetActive(visible);
        }
    }
}
