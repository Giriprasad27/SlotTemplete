using System.Collections;
using System.Collections.Generic;
using SlotTemplate.Core.Model;
using SlotTemplate.Data;
using SlotTemplate.Presentation.Reels;
using UnityEngine;

namespace SlotTemplate.Presentation.Wins
{
    /// <summary>
    /// Shows wins after the reels land: first every winning symbol at once, then each win line in turn,
    /// looping until <see cref="Clear"/> is called.
    /// </summary>
    public sealed class WinPresenter : MonoBehaviour
    {
        [SerializeField] private ReelsPresenter reels;
        [SerializeField] private SlotMachineConfig config;
        [SerializeField] private LineRenderer lineRenderer;
        [Tooltip("Pushes the line towards the camera so it draws over the symbols.")]
        [SerializeField] private float lineDepthOffset = -0.6f;

        private Coroutine _routine;

        public void Show(SpinOutcome outcome)
        {
            Clear();
            if (outcome == null || !outcome.IsWin) return;
            _routine = StartCoroutine(ShowRoutine(outcome));
        }

        public void Clear()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            reels.ClearHighlights();
            HideLine();
        }

        private IEnumerator ShowRoutine(SpinOutcome outcome)
        {
            float displayTime = reels.Settings.winLineDisplayTime;

            foreach (var win in outcome.LineWins) reels.Highlight(win.Positions);
            foreach (var win in outcome.ScatterWins) reels.Highlight(win.Positions);
            yield return new WaitForSeconds(displayTime);

            // A single win is already fully shown above; only cycle when there is more than one.
            int winCount = outcome.LineWins.Count + outcome.ScatterWins.Count;
            if (winCount < 2)
            {
                if (outcome.LineWins.Count == 1) DrawPayline(outcome.LineWins[0]);
                yield break;
            }

            while (true)
            {
                foreach (var win in outcome.LineWins)
                {
                    reels.ClearHighlights();
                    reels.Highlight(win.Positions);
                    DrawPayline(win);
                    yield return new WaitForSeconds(displayTime);
                }

                foreach (var win in outcome.ScatterWins)
                {
                    reels.ClearHighlights();
                    HideLine();
                    reels.Highlight(win.Positions);
                    yield return new WaitForSeconds(displayTime);
                }
            }
        }

        private void DrawPayline(LineWin win)
        {
            if (lineRenderer == null || config == null) return;

            var payline = config.Paylines[win.PaylineIndex];
            var points = new List<Vector3>(payline.rows.Length);
            for (int reel = 0; reel < payline.rows.Length; reel++)
            {
                var point = reels.GetSymbolWorldPosition(new GridPosition(reel, payline.rows[reel]));
                point.z += lineDepthOffset;
                points.Add(point);
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            lineRenderer.startColor = payline.color;
            lineRenderer.endColor = payline.color;
            lineRenderer.enabled = true;
        }

        private void HideLine()
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
        }
    }
}
