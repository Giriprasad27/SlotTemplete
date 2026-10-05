using SlotTemplate.Presentation.Skin;
using UnityEngine;

namespace SlotTemplate.Presentation.Reels
{
    /// <summary>One symbol on a reel: swaps mesh and material, and pulses when part of a win.</summary>
    public sealed class SymbolView : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private float highlightScale = 1.2f;
        [SerializeField] private float highlightSpeed = 6f;
        [SerializeField] private float idleRotationSpeed;

        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private bool _highlighted;
        private SymbolSkin.Entry _entry;

        private void Awake()
        {
            if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            _baseScale = transform.localScale;
            _baseRotation = transform.localRotation;
        }

        public void SetSymbol(SymbolSkin.Entry entry)
        {
            if (entry == _entry) return;
            _entry = entry;
            if (entry == null) return;

            if (meshFilter != null && entry.mesh != null) meshFilter.sharedMesh = entry.mesh;
            if (meshRenderer != null && entry.material != null) meshRenderer.sharedMaterial = entry.material;
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
