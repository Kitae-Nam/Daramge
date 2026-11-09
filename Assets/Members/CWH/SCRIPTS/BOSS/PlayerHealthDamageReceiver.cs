using CHG._02_Scripts.Player;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;

namespace CWH.BossIntegration
{
    /// <summary>
    /// 보스의 피해 계약을 실제 플레이어 체력 모듈로 변환한다(Adapter).
    /// Assembly-CSharp에서 두 어셈블리를 연결하므로 보스 Runtime은 CHG 구현에 의존하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        private PlayerHealthModule _health;
        public bool HasHealth => _health != null;

        public void Bind(PlayerHealthModule health) => _health = health;

        public float ReceiveDamage(float baseDamage)
        {
            // ApplyDamage(0)도 최소 1 피해로 반올림하므로 무효/0 피해는 어댑터에서 거른다.
            if (!isActiveAndEnabled || _health == null || !_health.isActiveAndEnabled ||
                _health.CurrentHealth <= 0 || baseDamage <= 0f ||
                float.IsNaN(baseDamage) || float.IsInfinity(baseDamage)) return 0f;

            int before = _health.CurrentHealth;
            _health.ApplyDamage(baseDamage);
            return Mathf.Max(0, before - _health.CurrentHealth);
        }
    }
}
