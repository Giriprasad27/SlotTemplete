using System;
using System.Collections.Generic;
using SlotTemplate.Core.Spin;
using UnityEngine;

namespace SlotTemplate.DebugTools
{
    /// <summary>
    /// On-screen cheat buttons for development builds. Each cheat forces the next paid spin to an outcome
    /// matching its predicate. Bootstrap registers feature cheats, so this assembly never references features.
    /// </summary>
    public sealed class DebugCheatMenu : MonoBehaviour
    {
        private readonly List<(string label, Func<SpinResult, bool> predicate)> _cheats = new List<(string, Func<SpinResult, bool>)>();
        private ForcedOutcomeProvider _provider;
        private Func<SpinRequest> _currentRequest;
        private bool _open;
        private string _status = "";

        public IEnumerable<string> CheatLabels
        {
            get { foreach (var cheat in _cheats) yield return cheat.label; }
        }

        public void Initialize(ForcedOutcomeProvider provider, Func<SpinRequest> currentRequest)
        {
            _provider = provider;
            _currentRequest = currentRequest;
            _cheats.Clear();
            AddCheat("Force 50x win", r => r.TotalWin >= r.Request.TotalBet * 50);
            AddCheat("Force any win", r => r.IsWin);
            AddCheat("Force no win", r => !r.IsWin);
        }

        public void AddCheat(string label, Func<SpinResult, bool> predicate) => _cheats.Add((label, predicate));

        public bool Run(string label)
        {
            foreach (var cheat in _cheats)
            {
                if (cheat.label != label) continue;

                bool found = _provider.ForceWhere(_currentRequest(), cheat.predicate);
                _status = found ? $"Next spin: {label}" : $"Nothing found for: {label}";
                Debug.Log($"[Slot cheat] {_status}");
                return found;
            }
            return false;
        }

        private void OnGUI()
        {
            if (_provider == null) return;

            if (GUI.Button(new Rect(10, 10, 70, 28), _open ? "Close" : "Cheats")) _open = !_open;
            if (!_open) return;

            GUILayout.BeginArea(new Rect(10, 44, 230, 40 + 30 * _cheats.Count), GUI.skin.box);
            foreach (var cheat in _cheats)
            {
                if (GUILayout.Button(cheat.label)) Run(cheat.label);
            }
            GUILayout.Label($"Queued: {_provider.QueuedCount}  {_status}");
            GUILayout.EndArea();
        }
    }
}
