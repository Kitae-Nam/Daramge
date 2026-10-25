using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using UnityEngine;

namespace Memebers.CWH.SCRIPTS.TEST
{
    /// <summary>실제 플레이어 체력 연결 전, 테스트 블록이 받은 피해를 Inspector와 로그로 확인한다.</summary>
    [DisallowMultipleComponent]
    public sealed class TestDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [SerializeField, Min(1f)] private float _maxHealth = 500f;
        [SerializeField] private float _currentHealth;
        private BossHealthModel _health;
        public float CurrentHealth => _health != null ? _health.CurrentHealth : _maxHealth;
        public bool IsDead => _health != null && _health.IsDead;

        private void Awake()
        {
            // 기존 수치 계산을 재사용하며, 이 컴포넌트는 테스트 표시만 담당한다.
            _health = new BossHealthModel(_maxHealth);
            _currentHealth = _health.CurrentHealth;
        }

        public float ReceiveDamage(float baseDamage)
        {
            float applied = _health.ReceiveDamage(baseDamage);
            _currentHealth = _health.CurrentHealth;
            Debug.Log($"플레이어 테스트 피해: {applied}, 남은 체력: {_currentHealth}/{_maxHealth}", this);
            return applied;
        }
    }
}
