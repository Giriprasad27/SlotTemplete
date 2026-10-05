using System;
using System.Collections.Generic;
using SlotTemplate.Core.Model;
using UnityEngine;

namespace SlotTemplate.Data
{
    /// <summary>Art and pay data for one symbol.</summary>
    [CreateAssetMenu(fileName = "Symbol", menuName = "Slot Template/Symbol", order = 0)]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [Serializable]
        public struct Payout
        {
            [Min(1)] public int count;

            [Tooltip("Line wins multiply the line bet; scatter wins multiply the total bet.")]
            [Min(0)] public long multiplier;
        }

        [SerializeField] private string displayName;
        [SerializeField] private SymbolKind kind = SymbolKind.Regular;

        [Header("Visuals")]
        [Tooltip("3D mesh shown on the reel. Leave empty to keep the SymbolView's own mesh.")]
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [Tooltip("Optional 2D icon, e.g. for a paytable screen.")]
        [SerializeField] private Sprite icon;

        [Header("Pays")]
        [SerializeField] private List<Payout> payouts = new List<Payout>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public SymbolKind Kind => kind;
        public Mesh Mesh => mesh;
        public Material Material => material;
        public Sprite Icon => icon;
        public IReadOnlyList<Payout> Payouts => payouts;

#if UNITY_EDITOR
        /// <summary>Editor-only setup used by the sample scene builder.</summary>
        public void EditorSetup(string newDisplayName, SymbolKind newKind, Mesh newMesh, Material newMaterial, params Payout[] newPayouts)
        {
            displayName = newDisplayName;
            kind = newKind;
            mesh = newMesh;
            material = newMaterial;
            payouts = new List<Payout>(newPayouts);
        }
#endif
    }
}
