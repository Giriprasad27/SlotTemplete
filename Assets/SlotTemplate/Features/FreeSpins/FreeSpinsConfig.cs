using System.Collections.Generic;
using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Features;
using UnityEngine;

namespace SlotTemplate.Features.FreeSpins
{
    [CreateAssetMenu(fileName = "FreeSpinsConfig", menuName = "Slot Template/Features/Free Spins", order = 10)]
    public sealed class FreeSpinsConfig : SlotFeatureConfig
    {
        [SerializeField] private FreeSpinsSettings settings = new FreeSpinsSettings();
        [SerializeField] private FreeSpinsPresenter presenterPrefab;

        public FreeSpinsSettings Settings => settings;

        public override ISpinRule CreateRule(SlotMath math) => new FreeSpinsRule(math, settings);

        public override ISlotFeature CreateFeature(ISpinRule rule, FeatureBuildContext context)
        {
            IFreeSpinsPresenter presenter = null;
            if (presenterPrefab != null)
            {
                var instance = Instantiate(presenterPrefab, context.UiRoot, false);
                instance.Hide();
                presenter = instance;
            }

            return new FreeSpinsFeature(settings, presenter);
        }

        public override void Validate(SlotMath math, List<string> errors)
        {
            if (!math.Paytable.TryIndexOf(settings.triggerSymbolId, out _))
                errors.Add($"Free spins trigger symbol '{settings.triggerSymbolId}' is not in the symbol list.");
            if (!math.HasReelSet(settings.reelSetId))
                errors.Add($"Free spins reel set '{settings.reelSetId}' does not exist.");
        }

#if UNITY_EDITOR
        public void EditorSetup(FreeSpinsSettings newSettings, FreeSpinsPresenter newPresenterPrefab)
        {
            settings = newSettings;
            presenterPrefab = newPresenterPrefab;
        }
#endif
    }
}
