using System;
using System.Collections;
using System.Collections.Generic;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;
using SlotTemplate.Data;
using UnityEngine;

namespace SlotTemplate.Presentation.Reels
{
    /// <summary>Drives all reels for one spin: start together, stop left to right, report when settled.</summary>
    public sealed class ReelsPresenter : MonoBehaviour
    {
        [SerializeField] private List<ReelView> reels = new List<ReelView>();
        [SerializeField] private ReelAnimationSettings settings;

        private Coroutine _spinRoutine;
        private bool _quickStopRequested;
        private int _stoppedCount;
        private Action _onAllStopped;

        public IReadOnlyList<ReelView> Reels => reels;
        public ReelAnimationSettings Settings => settings;
        public bool IsSpinning => _spinRoutine != null;

        /// <summary>Raised as each reel lands, with its index. Hook reel-stop sounds here.</summary>
        public event Action<int> ReelStopped;

        public void Initialize(SlotRules rules, IReadOnlyList<SymbolDefinition> symbols)
        {
            if (reels.Count != rules.ReelCount)
                throw new InvalidOperationException($"Scene has {reels.Count} reel views but the config defines {rules.ReelCount} reels.");

            for (int i = 0; i < reels.Count; i++)
            {
                int index = i;
                reels[i].Initialize(rules.Reels[i], symbols, rules.RowCount, settings, UnityEngine.Random.Range(0, rules.Reels[i].Length));
                reels[i].Stopped += _ => OnReelStopped(index);
            }
        }

        /// <summary>Spins every reel and lands on <paramref name="grid"/>'s stops.</summary>
        public void Spin(SpinGrid grid, Action onAllStopped)
        {
            if (IsSpinning) throw new InvalidOperationException("Reels are already spinning.");

            ClearHighlights();
            _quickStopRequested = false;
            _stoppedCount = 0;
            _onAllStopped = onAllStopped;
            _spinRoutine = StartCoroutine(SpinRoutine(grid));
        }

        /// <summary>Lands all remaining reels as soon as possible ("slam stop").</summary>
        public void QuickStop() => _quickStopRequested = true;

        public void Highlight(IEnumerable<GridPosition> positions)
        {
            foreach (var position in positions)
                reels[position.Reel].GetSymbolView(position.Row).SetHighlighted(true);
        }

        public void ClearHighlights()
        {
            foreach (var reel in reels) reel.ClearHighlights();
        }

        public Vector3 GetSymbolWorldPosition(GridPosition position) => reels[position.Reel].GetRowWorldPosition(position.Row);

        private IEnumerator SpinRoutine(SpinGrid grid)
        {
            foreach (var reel in reels) reel.StartSpin();

            float timer = 0f;
            while (timer < settings.minimumSpinTime && !_quickStopRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StopAt(grid.Stops[i]);

                float wait = 0f;
                while (wait < settings.reelStopInterval && !_quickStopRequested)
                {
                    wait += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private void OnReelStopped(int index)
        {
            ReelStopped?.Invoke(index);

            _stoppedCount++;
            if (_stoppedCount < reels.Count) return;

            _spinRoutine = null;
            var callback = _onAllStopped;
            _onAllStopped = null;
            callback?.Invoke();
        }
    }
}
