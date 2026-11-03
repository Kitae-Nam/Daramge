using System;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.Indicator
{
    public abstract class AbstractQuadRangeIndicator : MonoBehaviour, ISkillRangeIndicator
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        [SerializeField] private bool anchorAtStart;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.7f;

        private Renderer _quadRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _originLocalPosition;
        private Vector2 _coverage;

        private void Awake()
        {
            _quadRenderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _originLocalPosition = transform.localPosition;
            _coverage = GetCoverage(_quadRenderer.sharedMaterial);
            Hide();
        }

        public void Show()
        {
            SetFloat(OpacityId, opacity);
            SetProgress(0f);
            _quadRenderer.enabled = true;
        }

        public void Hide() => _quadRenderer.enabled = false;
        
        public void SetSize(Vector2 size)
        {
            transform.localScale = new Vector3(size.x / _coverage.x, size.y / _coverage.y, 1f);
            if (anchorAtStart)
                transform.localPosition = _originLocalPosition + Vector3.forward * (size.y * 0.5f);
        }
        
        public void SetProgress(float progress) => SetFloat(ProgressId, Mathf.Clamp01(progress));
        
        protected abstract Vector2 GetCoverage(Material material);
        protected static float GetFloatOrDefault(Material material, string propertyName, float defaultValue) 
            => material != null && material.HasProperty(propertyName) ? Mathf.Max(0.01f, material.GetFloat(propertyName)) : defaultValue;

        private void SetFloat(int propertyId, float value)
        {
            _quadRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(propertyId, value);
            _quadRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}