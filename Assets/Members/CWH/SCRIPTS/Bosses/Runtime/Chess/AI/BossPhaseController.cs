using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>체력에 따른 페이즈 판단만 담당한다. 60% 이하는 2페이즈이며 되돌아가지 않는다.</summary>
    [RequireComponent(typeof(BossHealth))]
    public sealed class BossPhaseController : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 0.99f)] private float _phaseTwoThreshold = 0.6f;
        private BossHealth _health;
        public int CurrentPhase { get; private set; } = 1;
        public event Action<int> PhaseChanged;
        private void Awake() => _health = GetComponent<BossHealth>();
        private void OnEnable() { _health.HealthChanged += HandleHealth; HandleHealth(_health.CurrentHealth, _health.MaxHealth); }
        private void OnDisable() => _health.HealthChanged -= HandleHealth;
        private void HandleHealth(float current, float maximum)
        {
            int phase = current <= 0f ? 0 : current / maximum <= _phaseTwoThreshold ? 2 : 1;
            if (CurrentPhase == phase || (CurrentPhase == 2 && phase == 1)) return;
            CurrentPhase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
