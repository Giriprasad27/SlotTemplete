using System;
using System.Collections.Generic;
using SlotTemplate.Core.Rules;
using SlotTemplate.Presentation.Skin;
using UnityEngine;

namespace SlotTemplate.Presentation.Reels
{
    /// <summary>
    /// Scrolls a column of <see cref="SymbolView"/>s along a reel strip and lands on a requested stop.
    /// Position is measured in symbols: <c>position == n</c> means strip index n is in the top row.
    /// The reel spins downwards, so position decreases while spinning.
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        private enum Phase { Idle, Spinning, Landing, Bouncing }

        [SerializeField] private SymbolView symbolPrefab;
        [Tooltip("Hide symbols whose centre is outside the window. Turn off if a mask or frame already covers them.")]
        [SerializeField] private bool hideOutsideWindow = true;

        private readonly List<SymbolView> _views = new List<SymbolView>();
        private ReelStrip _strip;
        private IReadOnlyList<SymbolSkin.Entry> _skinByIndex;
        private ReelAnimationSettings _settings;
        private int _rowCount;

        private Phase _phase = Phase.Idle;
        private float _position;
        private float _speed;
        private float _remaining;
        private float _bounceTimer;
        private float _bounceFrom;
        private int _targetStop;

        public bool IsSpinning => _phase != Phase.Idle;

        /// <summary>Raised when the reel settles after <see cref="StopAt"/> (not after <see cref="SnapTo"/>).</summary>
        public event Action<ReelView> Landed;

        public void Initialize(ReelStrip strip, IReadOnlyList<SymbolSkin.Entry> skinByIndex, int rowCount, ReelAnimationSettings settings, int initialStop)
        {
            _strip = strip ?? throw new ArgumentNullException(nameof(strip));
            _skinByIndex = skinByIndex ?? throw new ArgumentNullException(nameof(skinByIndex));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _rowCount = rowCount;
            _position = strip.Wrap(initialStop);
            _phase = Phase.Idle;

            BuildViews();
            Refresh();
        }

        public void StartSpin()
        {
            _phase = Phase.Spinning;
            _speed = 0f;
        }

        /// <summary>
        /// Lands on <paramref name="stop"/> of <paramref name="strip"/> after scrolling at least one window,
        /// so the final symbols roll in rather than pop. Switching strips (e.g. into free spins) happens here.
        /// </summary>
        public void StopAt(ReelStrip strip, int stop)
        {
            if (_phase == Phase.Landing || _phase == Phase.Bouncing) return;
            if (_phase == Phase.Idle) StartSpin();

            if (strip != _strip)
            {
                _strip = strip;
                _position = Mod(_position, strip.Length);
            }

            _targetStop = strip.Wrap(stop);
            _speed = Mathf.Max(_speed, _settings.maxSpeed * 0.5f);

            float distance = Mod(_position - _targetStop, strip.Length);
            while (distance < _rowCount + 1) distance += strip.Length;

            _remaining = distance + _settings.bounceDistance;
            _phase = Phase.Landing;
        }

        /// <summary>Jumps straight to the final position (skip / resume).</summary>
        public void SnapTo(ReelStrip strip, int stop)
        {
            _strip = strip;
            _targetStop = strip.Wrap(stop);
            _position = _targetStop;
            _phase = Phase.Idle;
            Refresh();
        }

        /// <summary>The view showing <paramref name="row"/>. Only meaningful while idle.</summary>
        public SymbolView GetSymbolView(int row) => _views[row + 1];

        public void ClearHighlights()
        {
            foreach (var view in _views) view.SetHighlighted(false);
        }

        public Vector3 GetRowWorldPosition(int row) => transform.TransformPoint(RowToLocal(row));

        private Vector3 RowToLocal(float row) => new Vector3(0f, ((_rowCount - 1) * 0.5f - row) * _settings.symbolSpacing, 0f);

        private void Update()
        {
            if (_phase == Phase.Idle || _strip == null) return;

            float dt = Time.deltaTime;
            switch (_phase)
            {
                case Phase.Spinning:
                    Accelerate(dt);
                    _position -= _speed * dt;
                    break;

                case Phase.Landing:
                    Accelerate(dt);
                    float step = Mathf.Min(_speed * dt, _remaining);
                    _position -= step;
                    _remaining -= step;
                    if (_remaining <= 0f)
                    {
                        _phase = Phase.Bouncing;
                        _bounceTimer = 0f;
                        _bounceFrom = _position;
                    }
                    break;

                case Phase.Bouncing:
                    _bounceTimer += dt;
                    float t = Mathf.Clamp01(_bounceTimer / _settings.bounceDuration);
                    float eased = 1f - (1f - t) * (1f - t);
                    _position = _bounceFrom + _settings.bounceDistance * eased;
                    if (t >= 1f)
                    {
                        _position = _targetStop;
                        _phase = Phase.Idle;
                        Refresh();
                        Landed?.Invoke(this);
                        return;
                    }
                    break;
            }

            _position = Mod(_position, _strip.Length);
            Refresh();
        }

        private void Accelerate(float dt)
        {
            float rate = _settings.accelerationTime > 0f ? _settings.maxSpeed / _settings.accelerationTime : float.MaxValue;
            _speed = Mathf.Min(_settings.maxSpeed, _speed + rate * dt);
        }

        private void BuildViews()
        {
            foreach (var view in _views)
            {
                if (view != null) Destroy(view.gameObject);
            }
            _views.Clear();

            // One spare view above and below the window so symbols scroll in and out smoothly.
            for (int i = 0; i < _rowCount + 2; i++)
            {
                var view = Instantiate(symbolPrefab, transform);
                view.name = $"Symbol_{i - 1}";
                _views.Add(view);
            }
        }

        private void Refresh()
        {
            int baseIndex = Mathf.FloorToInt(_position);
            float fraction = _position - baseIndex;

            for (int i = 0; i < _views.Count; i++)
            {
                int slot = i - 1;
                float row = slot - fraction;
                var view = _views[i];

                view.transform.localPosition = RowToLocal(row);
                view.SetSymbol(_skinByIndex[_strip.GetSymbol(baseIndex + slot)]);
                if (hideOutsideWindow) view.SetVisible(row > -0.5f && row < _rowCount - 0.5f);
            }
        }

        private static float Mod(float value, float length)
        {
            float result = value % length;
            return result < 0f ? result + length : result;
        }
    }
}
