using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Presentation;
using SlotTemplate.Presentation.Reels;
using UnityEngine;

namespace SlotTemplate.Presentation.Wins
{
    /// <summary>
    /// Highlights every winning symbol, then keeps cycling through the individual win lines in the
    /// background until <see cref="Clear"/>.
    /// </summary>
    public sealed class WinPresenter : MonoBehaviour, IWinPresenter
    {
        [SerializeField] private ReelsPresenter reels;
        [SerializeField] private LineRenderer lineRenderer;
        [Tooltip("Pushes the line towards the camera so it draws over the symbols.")]
        [SerializeField] private float lineDepthOffset = -0.6f;
        [SerializeField] private Gradient paylineColors = DefaultColors();

        private SlotMath _math;
        private CancellationTokenSource _cycle;

        public void Initialize(SlotMath math) => _math = math;

        public async UniTask ShowWins(SpinResult result, SkipSignal skip, CancellationToken ct)
        {
            Clear();
            if (!result.IsWin) return;

            foreach (var win in result.Wins) reels.Highlight(win.Positions);
            await skip.WaitAsync(reels.Settings.winDisplayTime, ct);

            if (result.Wins.Count > 1)
            {
                _cycle = CancellationTokenSource.CreateLinkedTokenSource(ct);
                CycleWins(result.Wins, _cycle.Token).Forget();
            }
            else if (result.Wins[0].Kind == WinKind.Line)
            {
                DrawPayline(result.Wins[0]);
            }
        }

        public void Clear()
        {
            if (_cycle != null)
            {
                _cycle.Cancel();
                _cycle.Dispose();
                _cycle = null;
            }

            reels.ClearHighlights();
            HideLine();
        }

        private void OnDestroy() => Clear();

        private async UniTaskVoid CycleWins(IReadOnlyList<Win> wins, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                foreach (var win in wins)
                {
                    reels.ClearHighlights();
                    reels.Highlight(win.Positions);
                    if (win.Kind == WinKind.Line) DrawPayline(win);
                    else HideLine();

                    if (await UniTask.Delay(System.TimeSpan.FromSeconds(reels.Settings.winLineCycleTime), cancellationToken: ct).SuppressCancellationThrow())
                        return;
                }
            }
        }

        private void DrawPayline(Win win)
        {
            if (lineRenderer == null || _math == null || win.PaylineIndex < 0) return;

            var payline = _math.Paylines[win.PaylineIndex];
            var points = new Vector3[payline.Rows.Count];
            for (int reel = 0; reel < points.Length; reel++)
            {
                var point = reels.GetSymbolWorldPosition(new GridPosition(reel, payline.RowOnReel(reel)));
                point.z += lineDepthOffset;
                points[reel] = point;
            }

            var color = paylineColors.Evaluate(_math.Paylines.Count > 1 ? (float)win.PaylineIndex / (_math.Paylines.Count - 1) : 0f);
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.enabled = true;
        }

        private void HideLine()
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        private static Gradient DefaultColors()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.3f, 1f, 0.5f), 0.33f),
                    new GradientColorKey(new Color(0.3f, 0.7f, 1f), 0.66f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.8f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
