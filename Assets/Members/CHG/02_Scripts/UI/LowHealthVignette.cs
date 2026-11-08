using System;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CHG._02_Scripts.UI
{
    public class LowHealthVignette : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [SerializeField, Min(1)] private int lowHealthThreshold = 2;
        [SerializeField, Range(0f, 1f)] private float minIntensity = 0.25f;
        [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.5f;
        [SerializeField] private float fadeSpeed = 2f;

        private Vignette _vignette;
        private PlayerHealthModule _healthModule;
        private float _targetIntensity;

        private void Awake()
        {
            Debug.Assert(volume != null, $"volume is null : {gameObject.name}");
            bool hasVignette = volume.profile.TryGet(out _vignette);
            Debug.Assert(hasVignette, $"vignette is null : {gameObject.name}");
            EventBus<CharacterChangeStartEvent>.Subscribe(HandleCharacterChangeStarted);
        }

        private void OnDestroy()
        {
            EventBus<CharacterChangeStartEvent>.Subscribe(HandleCharacterChangeStarted);
            if (_healthModule != null)
                _healthModule.OnHealthChanged -= HandleHealthChanged;
        }

        private void Update()
        {
            _vignette.intensity.value = Mathf.MoveTowards(
                _vignette.intensity.value, _targetIntensity, fadeSpeed * Time.unscaledDeltaTime);
        }

        private void HandleCharacterChangeStarted(CharacterChangeStartEvent evt)
        {
            if (_healthModule != null)
                _healthModule.OnHealthChanged -= HandleHealthChanged;
            
            _healthModule = evt.Current.GetModule<PlayerHealthModule>();
            Debug.Assert(_healthModule != null, $"healthModule is null : {_healthModule == null}");

            _healthModule.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_healthModule.CurrentHealth, _healthModule.MaxHealth);
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (current > lowHealthThreshold)
            {
                _targetIntensity = 0f;
                return;
            }

            float danger = 1f - (float)current / lowHealthThreshold;
            _targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, danger);
        }
    }
}