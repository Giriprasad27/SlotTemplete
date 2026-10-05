using UnityEngine;

namespace SlotTemplate.Data
{
    /// <summary>Timing and feel of the reel spin. Distances are in symbol heights.</summary>
    [CreateAssetMenu(fileName = "ReelAnimationSettings", menuName = "Slot Template/Reel Animation Settings", order = 2)]
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
        [Tooltip("How far a reel overshoots before settling.")]
        [Min(0f)] public float bounceDistance = 0.25f;
        [Min(0.01f)] public float bounceDuration = 0.18f;

        [Header("Wins")]
        [Tooltip("Seconds each win line stays on screen while cycling through wins.")]
        [Min(0.1f)] public float winLineDisplayTime = 1.2f;
    }
}
