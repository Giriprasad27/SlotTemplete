using SlotTemplate.Core.Rules;
using SlotTemplate.Core.Spin;
using SlotTemplate.Flow.Features;
using UnityEngine;

namespace SlotTemplate.Features
{
    /// <summary>
    /// Base for a feature's config asset. List configs in the game's SlotDefinition and the bootstrap
    /// registers each rule with the spin engine and each feature with the round runner.
    /// </summary>
    public abstract class SlotFeatureConfig : ScriptableObject
    {
        /// <summary>The math half. Also used by the RTP simulator, so it must not touch the scene.</summary>
        public abstract ISpinRule CreateRule(SlotMath math);

        /// <summary>The playable half. <paramref name="rule"/> is the instance returned by <see cref="CreateRule"/>.</summary>
        public abstract ISlotFeature CreateFeature(ISpinRule rule, FeatureBuildContext context);

        /// <summary>Problems with this config; empty when valid.</summary>
        public virtual void Validate(SlotMath math, System.Collections.Generic.List<string> errors) { }
    }

    /// <summary>Scene objects a feature may need when it is created.</summary>
    public sealed class FeatureBuildContext
    {
        /// <summary>Canvas or transform to put feature UI under.</summary>
        public Transform UiRoot { get; }

        public FeatureBuildContext(Transform uiRoot) => UiRoot = uiRoot;
    }
}
