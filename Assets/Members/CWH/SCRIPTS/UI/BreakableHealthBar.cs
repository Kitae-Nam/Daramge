using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.BossUI.Members.CWH.SCRIPTS.UI
{
    public class BreakableHealthBar : MonoBehaviour
    {
        [SerializeField] private BreakableHealth _health;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Canvas _canvas;
        
        private float _fullWidth;

        private void Awake()
        {
            if (_fill != null)
                _fullWidth = _fill.rect.width;
        }

        private void OnEnable()
        {
            if (_health == null || _fill == null) return;

            _health.HealthChanged += UpdateWidth;
            UpdateWidth(_health.CurrentHealth, _health.MaxHealth);
        }
        private void LateUpdate()
        {
            if (_canvas == null)
                return;

            _canvas.enabled = _health != null &&
                              _health.isActiveAndEnabled &&
                              !_health.IsDead &&
                              _health.CanTakeDamage &&
                              _health.CurrentHealth < _health.MaxHealth;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.HealthChanged -= UpdateWidth;
            
            if (_canvas != null)
                _canvas.enabled = false;
        }

        private void UpdateWidth(float cur, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            
            _fill.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, ratio *  _fullWidth);
        }
        
    }
}