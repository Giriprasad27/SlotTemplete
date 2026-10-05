using System.Collections.Generic;
using SlotTemplate.Bootstrap;
using SlotTemplate.Core.Simulation;
using SlotTemplate.Core.Spin;
using UnityEditor;
using UnityEngine;

namespace SlotTemplate.Editor
{
    /// <summary>Runs millions of spins of a SlotDefinition's math (base game and features) without a scene.</summary>
    public sealed class RtpSimulatorWindow : EditorWindow
    {
        private const long Chunk = 50000;

        private SlotDefinition _definition;
        private long _spins = 1000000;
        private int _seed = 1;
        private int _betIndex;
        private string _report = "";
        private Vector2 _scroll;

        [MenuItem("Tools/Slot Template/RTP Simulator")]
        public static void Open() => GetWindow<RtpSimulatorWindow>("RTP Simulator");

        private void OnEnable()
        {
            if (_definition == null && Selection.activeObject is SlotDefinition selected) _definition = selected;
        }

        private void OnGUI()
        {
            _definition = (SlotDefinition)EditorGUILayout.ObjectField("Slot definition", _definition, typeof(SlotDefinition), false);
            _spins = EditorGUILayout.LongField("Spins", _spins);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (_definition != null && _definition.LineBets.Count > 0)
                _betIndex = EditorGUILayout.IntSlider("Bet level", _betIndex, 0, _definition.LineBets.Count - 1);

            using (new EditorGUI.DisabledScope(_definition == null || _spins <= 0))
            {
                if (GUILayout.Button("Run")) Run();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Run()
        {
            var errors = _definition.Validate();
            if (errors.Count > 0)
            {
                _report = "Definition is invalid:\n- " + string.Join("\n- ", errors);
                return;
            }

            var math = _definition.BuildMath();
            var rules = _definition.BuildRules(math);
            long lineBet = _definition.LineBets[Mathf.Clamp(_betIndex, 0, _definition.LineBets.Count - 1)];
            var request = new SpinRequest(lineBet, math.Paylines.Count);
            var simulator = new RtpSimulator(math, rules, request, _seed);

            var started = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                for (long done = 0; done < _spins; done += Chunk)
                {
                    simulator.Run(System.Math.Min(Chunk, _spins - done));
                    if (EditorUtility.DisplayCancelableProgressBar("RTP Simulator", $"{done:N0} / {_spins:N0} spins", (float)done / _spins))
                        break;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var report = simulator.Report;
            _report = $"{_definition.name}, line bet {lineBet}, {math.Paylines.Count} lines, seed {_seed}\n"
                      + report
                      + $"Max win:        {report.MaxWinMultiplier(request.TotalBet):N0}x bet\n"
                      + $"Time:           {started.Elapsed.TotalSeconds:F1}s";
        }
    }
}
