using SlotTemplate.Data;
using UnityEngine;

namespace SlotTemplate.Presentation.Reels
{
    /// <summary>One symbol on a reel: swaps its mesh and material, and pulses when part of a win.</summary>
    public sealed class SymbolView : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private float highlightScale = 1.2f;
        [SerializeField] private float highlightSpeed = 6f;
        [SerializeField] private float idleRotationSpeed = 0f;

        private Vector3 _baseScale;
        private bool _highlighted;
        private Quaternion _baseRotation;

        public SymbolDefinition Symbol { get; private set; }

        private void Awake()
        {
            if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            _baseScale = transform.localScale;
            _baseRotation = transform.localRotation;
        }

        public void SetSymbol(SymbolDefinition symbol)
        {
            if (symbol == Symbol) return;
            Symbol = symbol;

            if (symbol == null) return;
            if (meshFilter != null && symbol.Mesh != null) meshFilter.sharedMesh = symbol.Mesh;
            if (meshRenderer != null && symbol.Material != null) meshRenderer.sharedMaterial = symbol.Material;
        }

        public void SetVisible(bool visible)
        {
            if (meshRenderer != null && meshRenderer.enabled != visible) meshRenderer.enabled = visible;
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
            if (!highlighted)
            {
                transform.localScale = _baseScale;
                transform.localRotation = _baseRotation;
            }
        }

        private void Update()
        {
            if (_highlighted)
            {
                float pulse = (Mathf.Sin(Time.time * highlightSpeed) + 1f) * 0.5f;
                transform.localScale = _baseScale * Mathf.Lerp(1f, highlightScale, pulse);
                transform.Rotate(Vector3.up, 180f * Time.deltaTime, Space.Self);
            }
            else if (idleRotationSpeed != 0f)
            {
                transform.Rotate(Vector3.up, idleRotationSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
