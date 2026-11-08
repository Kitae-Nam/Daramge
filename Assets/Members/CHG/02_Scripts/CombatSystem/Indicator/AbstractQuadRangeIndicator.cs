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
        [SerializeField, Range(0f, 1f)] private float flashOpacity = 1f;

        private Renderer _quadRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _originLocalPosition;
        private Vector2 _coverage;
        private float _fadeDuration;
        private float _fadeTimeLeft;
        private float _fadeTargetOpacity;
        private bool _hideAfterFade;

        private void Awake()
        {
            _quadRenderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _originLocalPosition = transform.localPosition;
            _coverage = GetCoverage(_quadRenderer.sharedMaterial);
            Hide();
        }

        private void Update()
        {
            if (_fadeTimeLeft <= 0f) return;

            _fadeTimeLeft -= Time.deltaTime;
            float percent = 1f - Mathf.Clamp01(_fadeTimeLeft / _fadeDuration);
            SetFloat(OpacityId, Mathf.Lerp(flashOpacity, _fadeTargetOpacity, percent));
            
            if (_fadeTimeLeft <= 0f && _hideAfterFade)
                Hide();
        }

        public void Show()
        {
            _fadeTimeLeft = 0f;
            SetFloat(OpacityId, opacity);
            SetProgress(0f);
            _quadRenderer.enabled = true;
        }

        public void Hide()
        {
            _fadeTimeLeft = 0f;
            _quadRenderer.enabled = false;
        }

        public void Pulse(float duration) => StartFade(opacity, duration, false); 

        public void Flash(float duration) 
        {
            SetProgress(1f);
            _quadRenderer.enabled = true;
            StartFade(0f, duration, true);
        }

        private void StartFade(float targetOpacity, float duration, bool hideAfterFade) 
        {
            _fadeDuration = Mathf.Max(duration, 0.01f);
            _fadeTimeLeft = _fadeDuration;
            _fadeTargetOpacity = targetOpacity;
            _hideAfterFade = hideAfterFade;
            SetFloat(OpacityId, flashOpacity);
        }
        
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