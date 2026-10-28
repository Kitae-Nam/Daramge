using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health
{
    /// <summary>기물/박힌 검의 체력. 수치는 기존 체력 모델이 계산하고 파괴 후 행동은 구독자가 결정한다.</summary>
    [DisallowMultipleComponent]
    public sealed class BreakableHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField, Min(1f)] private float _maxHealth = 80f;
        private BossHealthModel _model;
        private BossHealthModel Model => _model ?? (_model = new BossHealthModel(_maxHealth));
        public bool CanTakeDamage { get; set; } = true;
        public bool IsDead => Model.IsDead;
        public float CurrentHealth => Model.CurrentHealth;
        public event Action Broken;

        private void Awake() { Model.Died += OnBroken; }
        private void OnBroken() => Broken?.Invoke();
        public float ReceiveDamage(float damage) => CanTakeDamage && isActiveAndEnabled ? Model.ReceiveDamage(damage) : 0f;

#if UNITY_EDITOR
        [ContextMenu("테스트/파괴")]
        private void TestBreak() { if (Application.isPlaying) ReceiveDamage(float.MaxValue); }
#endif
    }
}
