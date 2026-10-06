using System;
using System.Collections.Generic;
using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Spin
{
    /// <summary>
    /// Everything decided for one spin. Rules add wins and attach their own typed results
    /// (e.g. <c>result.Set(new FreeSpinsResult(...))</c>), so this class never grows a field per feature.
    /// </summary>
    public sealed class SpinResult
    {
        private readonly List<Win> _wins = new List<Win>();
        private readonly Dictionary<Type, object> _slots = new Dictionary<Type, object>();

        public SpinRequest Request { get; }
        public Grid Grid { get; }
        public IReadOnlyList<Win> Wins => _wins;
        public long TotalWin { get; private set; }
        public bool IsWin => TotalWin > 0;

        public SpinResult(SpinRequest request, Grid grid)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public void AddWin(Win win)
        {
            if (win == null) throw new ArgumentNullException(nameof(win));
            _wins.Add(win);
            TotalWin += win.Payout;
        }

        public void Set<T>(T value) where T : class => _slots[typeof(T)] = value ?? throw new ArgumentNullException(nameof(value));

        public bool TryGet<T>(out T value) where T : class
        {
            if (_slots.TryGetValue(typeof(T), out var boxed))
            {
                value = (T)boxed;
                return true;
            }
            value = null;
            return false;
        }

        public T Get<T>() where T : class =>
            TryGet<T>(out var value) ? value : throw new KeyNotFoundException($"No {typeof(T).Name} on this result.");

        public bool Has<T>() where T : class => _slots.ContainsKey(typeof(T));
    }
}
