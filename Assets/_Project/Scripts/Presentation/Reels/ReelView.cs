using System;
using System.Collections.Generic;
using SlotTemplate.Core.Rules;
using SlotTemplate.Data;
using UnityEngine;

namespace SlotTemplate.Presentation.Reels
{
    /// <summary>
    /// Scrolls a column of <see cref="SymbolView"/>s along a reel strip and lands on a requested stop.
    /// Position is measured in symbols: <c>position == n</c> means strip index n sits in the top row.
    /// The reel spins downwards, so position decreases while spinning.
    /// </summary>
    public sealed class ReelView : MonoBehaviour
    {
        private enum Phase { Idle, Spinning, Landing, Bouncing }

        [SerializeField] private SymbolView symbolPrefab;
        [Tooltip("Optional parent for spawned symbols; defaults to this transform.")]
        [SerializeField] private Transform symbolRoot;
        [Tooltip("Hide symbols whose centre is outside the visible window. Turn off if a frame or mask already covers them.")]
        [SerializeField] private bool hideOutsideWindow = true;

        private readonly List<SymbolView> _views = new List<SymbolView>();
        private ReelStrip _strip;
        private IReadOnlyList<SymbolDefinition> _symbols;
        private ReelAnimationSettings _settings;
        private int _rowCount;

        private Phase _phase = Phase.Idle;
        private float _position;
        private float _speed;
        private float _remainingDistance;
        private float _bounceTimer;
        private float _bounceFrom;
        private int _targetStop;

        public bool IsSpinning => _phase != Phase.Idle;
        public int CurrentStop => _strip != null ? _strip.Wrap(Mathf.RoundToInt(_position)) : 0;

        /// <summary>Raised when the reel has fully settled on its target.</summary>
        public event Action<ReelView> Stopped;

        public void Initialize(ReelStrip strip, IReadOnlyList<SymbolDefinition> symbols, int rowCount, ReelAnimationSettings settings, int initialStop = 0)
        {
            _strip = strip ?? throw new ArgumentNullException(nameof(strip));
            _symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _rowCount = rowCount;
            _position = strip.Wrap(initialStop);
            _phase = Phase.Idle;

            BuildViews();
            Refresh();
        }

        /// <summary>Starts scrolling; the reel keeps spinning until <see cref="StopAt"/> is called.</summary>
        public void StartSpin()
        {
            if (_strip == null) return;
            _phase = Phase.Spinning;
            _speed = 0f;
        }

        /// <summary>Lands on <paramref name="stop"/> after scrolling at least one full window of real strip.</summary>
        public void StopAt(int stop)
        {
            if (_strip == null) return;
            if (_phase == Phase.Idle) StartSpin();
            if (_phase == Phase.Landing || _phase == Phase.Bouncing) return;

            _targetStop = _strip.Wrap(stop);
            _speed = Mathf.Max(_speed, _settings.maxSpeed * 0.5f);

            // Distance (downwards) from the current position to the target, plus enough extra travel
            // that the final symbols scroll in from above instead of popping into place.
            float distance = Mod(_position - _targetStop, _strip.Length);
            float minimumTravel = _rowCount + 1;
            while (distance < minimumTravel) distance += _strip.Length;

            _remainingDistance = distance + _settings.bounceDistance;
            _phase = Phase.Landing;
        }

        /// <summary>The view showing <paramref name="row"/>. Only meaningful while the reel is idle.</summary>
        public SymbolView GetSymbolView(int row) => _views[row + 1];

        public void ClearHighlights()
        {
            foreach (var view in _views) view.SetHighlighted(false);
        }

        /// <summary>Local position of a row's centre, relative to this reel.</summary>
        public Vector3 GetRowLocalPosition(float row)
        {
            float centreOffset = (_rowCount - 1) * 0.5f;
            return new Vector3(0f, (centreOffset - row) * _settings.symbolSpacing, 0f);
        }

        public Vector3 GetRowWorldPosition(int row) => transform.TransformPoint(GetRowLocalPosition(row));

        private void Update()
        {
            if (_phase == Phase.Idle) return;

            float dt = Time.deltaTime;
            switch (_phase)
            {
                case Phase.Spinning:
                    Accelerate(dt);
                    _position -= _speed * dt;
                    break;

                case Phase.Landing:
                    Accelerate(dt);
                    float step = Mathf.Min(_speed * dt, _remainingDistance);
                    _position -= step;
                    _remainingDistance -= step;
                    if (_remainingDistance <= 0f)
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
                        Stopped?.Invoke(this);
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

            var parent = symbolRoot != null ? symbolRoot : transform;

            // One spare view above and below the window so symbols can scroll in and out smoothly.
            for (int i = 0; i < _rowCount + 2; i++)
            {
                var view = Instantiate(symbolPrefab, parent);
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

                view.transform.localPosition = GetRowLocalPosition(row);
                view.SetSymbol(_symbols[_strip.GetSymbol(baseIndex + slot)]);

                if (hideOutsideWindow)
                    view.SetVisible(row > -0.5f && row < _rowCount - 0.5f);
            }
        }

        private static float Mod(float value, float length)
        {
            float result = value % length;
            return result < 0f ? result + length : result;
        }
    }
}
