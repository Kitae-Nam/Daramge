using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health
{
    /// <summary>Inspector 설정과 체력 모델을 연결한다. UI/페이즈는 이벤트를 구독해 별도로 구현한다.</summary>
    [DisallowMultipleComponent]
    public sealed class BossHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField, Min(1f)] private float _maxHealth = 1000f;
        private BossHealthModel _model;

        // 다른 컴포넌트의 Awake 순서에 의존하지 않도록 최초 접근 시에도 초기화한다.
        //Lazy Initialization (게으른 초기화) 
        private BossHealthModel Model => _model ?? (_model = new BossHealthModel(_maxHealth)); 
        public float MaxHealth => Model.MaxHealth;
        public float CurrentHealth => Model.CurrentHealth;
        public float HealthRatio => Model.HealthRatio;
        public bool IsDead => Model.IsDead;

        public event Action<float, float> HealthChanged
        {
            add => Model.HealthChanged += value;
            remove => Model.HealthChanged -= value;
        }

        public event Action Died
        {
            add => Model.Died += value;
            remove => Model.Died -= value;
        }

        private void Awake() { _ = Model; }

        public float ReceiveDamage(float baseDamage) => Model.ReceiveDamage(baseDamage);
        public void SetDamageMultiplier(object source, float multiplier) => Model.SetDamageMultiplier(source, multiplier);
        public void RemoveDamageMultiplier(object source) => Model.RemoveDamageMultiplier(source);
        
#if UNITY_EDITOR
       [ContextMenu("테스트/기본 피해 100")]
        private void TestSetDamage()
        {
            if (!Application.isPlaying) return;

            float appliedDamage = ReceiveDamage(100f);

            Debug.Log(
                $"실제 피해: {appliedDamage}, 현재 체력: {CurrentHealth}/{MaxHealth}",
                this);
        }
#endif
    }
}
