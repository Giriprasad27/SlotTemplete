using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SlotTemplate.Core.Model;
using SlotTemplate.Core.Rules;
using SlotTemplate.Flow.Presentation;
using SlotTemplate.Presentation.Skin;
using UnityEngine;
using Grid = SlotTemplate.Core.Model.Grid;

namespace SlotTemplate.Presentation.Reels
{
    /// <summary>All reels together: start at once, stop left to right, report when settled.</summary>
    public sealed class ReelsPresenter : MonoBehaviour, IReelPresenter
    {
        [SerializeField] private List<ReelView> reels = new List<ReelView>();
        [SerializeField] private ReelAnimationSettings settings;

        private SlotMath _math;

        public ReelAnimationSettings Settings => settings;

        /// <summary>Raised as each reel lands, with its index. Hook reel-stop sounds here.</summary>
        public event Action<int> ReelStopped;

        public void Initialize(SlotMath math, SymbolSkin skin, string initialReelSetId)
        {
            _math = math;
            if (reels.Count != math.ReelCount)
                throw new InvalidOperationException($"Scene has {reels.Count} reel views but the game has {math.ReelCount} reels.");

            var skinByIndex = new SymbolSkin.Entry[math.Paytable.Count];
            for (int i = 0; i < skinByIndex.Length; i++)
            {
                var id = math.Paytable[i].Id;
                skinByIndex[i] = skin != null ? skin.Find(id) : null;
                if (skinByIndex[i] == null) Debug.LogWarning($"[Slot] Symbol skin has no entry for '{id}'.", this);
            }

            var set = math.GetReelSet(initialReelSetId);
            for (int i = 0; i < reels.Count; i++)
            {
                int index = i;
                reels[i].Initialize(set.Reels[i], skinByIndex, math.RowCount, settings, UnityEngine.Random.Range(0, set.Reels[i].Length));
                reels[i].Landed += _ => ReelStopped?.Invoke(index);
            }
        }

        public async UniTask SpinTo(Grid grid, SkipSignal skip, CancellationToken ct)
        {
            var set = _math.GetReelSet(grid.ReelSetId);
            ClearHighlights();

            if (skip.IsRequested)
            {
                Snap(grid, set);
                return;
            }

            foreach (var reel in reels) reel.StartSpin();

            await skip.WaitAsync(settings.minimumSpinTime, ct);

            for (int i = 0; i < reels.Count && !skip.IsRequested; i++)
            {
                reels[i].StopAt(set.Reels[i], grid.Stops[i]);
                if (i < reels.Count - 1) await skip.WaitAsync(settings.reelStopInterval, ct);
            }

            await UniTask.WaitUntil(() => skip.IsRequested || !AnySpinning(), cancellationToken: ct);
            if (skip.IsRequested) Snap(grid, set);
        }

        public void Show(Grid grid) => Snap(grid, _math.GetReelSet(grid.ReelSetId));

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

        private bool AnySpinning()
        {
            foreach (var reel in reels)
            {
                if (reel.IsSpinning) return true;
            }
            return false;
        }

        private void Snap(Grid grid, ReelSet set)
        {
            for (int i = 0; i < reels.Count; i++) reels[i].SnapTo(set.Reels[i], grid.Stops[i]);
        }
    }
}
