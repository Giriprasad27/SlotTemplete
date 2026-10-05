using UnityEngine;

namespace SlotTemplate.Presentation
{
    /// <summary>Timing and feel of the reels and win display. Distances are in symbol heights.</summary>
    [CreateAssetMenu(fileName = "ReelAnimationSettings", menuName = "Slot Template/Reel Animation Settings", order = 3)]
    public sealed class ReelAnimationSettings : ScriptableObject
    {
        [Header("Layout")]
        [Tooltip("World-space distance between symbol centres on a reel.")]
        [Min(0.01f)] public float symbolSpacing = 1.2f;

        [Header("Spin")]
        [Tooltip("Top speed in symbols per second.")]
        [Min(0.1f)] public float maxSpeed = 18f;
        [Min(0f)] public float accelerationTime = 0.25f;
        [Tooltip("Minimum time all reels spin before the first one stops.")]
        [Min(0f)] public float minimumSpinTime = 0.8f;
        [Tooltip("Delay between consecutive reels stopping, left to right.")]
        [Min(0f)] public float reelStopInterval = 0.2f;

        [Header("Landing")]
        [Min(0f)] public float bounceDistance = 0.25f;
        [Min(0.01f)] public float bounceDuration = 0.18f;

        [Header("Wins")]
        [Tooltip("How long all wins are shown together before the round continues.")]
        [Min(0f)] public float winDisplayTime = 1f;
        [Tooltip("Seconds each win line stays on screen while cycling.")]
        [Min(0.1f)] public float winLineCycleTime = 1.2f;
    }
}
