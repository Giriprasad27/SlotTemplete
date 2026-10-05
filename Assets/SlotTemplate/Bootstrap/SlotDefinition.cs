using System.Collections.Generic;
using SlotTemplate.Core.Definition;
using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Spin;
using SlotTemplate.Features;
using SlotTemplate.Flow.Round;
using SlotTemplate.Presentation.Skin;
using UnityEngine;

namespace SlotTemplate.Bootstrap
{
    /// <summary>One game: its math, economy, features and skin. Everything a game folder needs to configure.</summary>
    [CreateAssetMenu(fileName = "SlotDefinition", menuName = "Slot Template/Slot Definition", order = 0)]
    public sealed class SlotDefinition : ScriptableObject
    {
        [SerializeField] private SlotDefinitionData math = new SlotDefinitionData();

        [Header("Economy")]
        [SerializeField] private long[] lineBets = { 1, 2, 5, 10 };
        [Min(0)] [SerializeField] private int defaultBetIndex;
        [Min(0)] [SerializeField] private long startingBalance = 1000;

        [Header("Features (played in this order)")]
        [SerializeField] private List<SlotFeatureConfig> features = new List<SlotFeatureConfig>();

        [Header("Look")]
        [SerializeField] private SymbolSkin skin;

        [Header("Play")]
        [SerializeField] private AutoplaySettings autoplay = new AutoplaySettings();
        [Tooltip("Same seed, same spins. Handy for reproducing a bug.")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 12345;
        [Tooltip("Save key; change it per game so games don't share a balance.")]
        [SerializeField] private string saveKey = "slot.save";

        public SlotDefinitionData Math => math;
        public IReadOnlyList<long> LineBets => lineBets;
        public int DefaultBetIndex => defaultBetIndex;
        public long StartingBalance => startingBalance;
        public IReadOnlyList<SlotFeatureConfig> Features => features;
        public SymbolSkin Skin => skin;
        public AutoplaySettings Autoplay => autoplay;
        public bool UseFixedSeed => useFixedSeed;
        public int Seed => seed;
        public string SaveKey => saveKey;

        public SlotMath BuildMath() => SlotMathFactory.Build(math);

        /// <summary>Built-in pay rules followed by each feature's rule, in feature order.</summary>
        public List<ISpinRule> BuildRules(SlotMath slotMath, List<ISpinRule> featureRules = null)
        {
            var rules = new List<ISpinRule> { new PaylineRule(), new ScatterPayRule() };
            foreach (var feature in features)
            {
                if (feature == null) continue;
                var rule = feature.CreateRule(slotMath);
                rules.Add(rule);
                featureRules?.Add(rule);
            }
            return rules;
        }

        public List<string> Validate()
        {
            var errors = SlotMathFactory.Validate(math);
            if (lineBets == null || lineBets.Length == 0) errors.Add("Add at least one line bet.");
            if (errors.Count > 0) return errors;

            var slotMath = SlotMathFactory.Build(math);
            if (!slotMath.HasReelSet(SpinRequest.BaseReelSet)) errors.Add($"Add a reel set with id '{SpinRequest.BaseReelSet}'.");
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i] == null) errors.Add($"Feature slot {i} is empty.");
                else features[i].Validate(slotMath, errors);
            }

            if (skin != null)
            {
                foreach (var symbol in math.symbols)
                {
                    if (skin.Find(symbol.id) == null) errors.Add($"Skin '{skin.name}' has no entry for symbol '{symbol.id}'.");
                }
            }
            return errors;
        }

#if UNITY_EDITOR
        public void EditorSetup(SlotDefinitionData newMath, long[] newLineBets, long newStartingBalance,
            List<SlotFeatureConfig> newFeatures, SymbolSkin newSkin, string newSaveKey)
        {
            math = newMath;
            lineBets = newLineBets;
            startingBalance = newStartingBalance;
            features = newFeatures;
            skin = newSkin;
            saveKey = newSaveKey;
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"[{name}] {error}", this);
        }
#endif
    }
}
