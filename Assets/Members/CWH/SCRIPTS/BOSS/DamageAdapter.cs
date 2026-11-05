using CHG._02_Scripts.CombatSystem;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;

namespace CWH.SCRIPTS.BOSS
{
    [DisallowMultipleComponent]
    public class DamageAdapter : MonoBehaviour, IDamageable
    {
        // 보스는 Health 검은 BreakHealth를 집어넣는다.
        [SerializeField] private MonoBehaviour _receiverComponent;

        private IDamageReceiver _receiver;

        private void Awake()
        {
            _receiver = _receiverComponent as IDamageReceiver;

            if (_receiver == null)
            {
                Debug.LogError(
                    "IDamageReceiver를 구현한 체력 컴포넌트를 연결해 주세요.",
                    this);
            }
        }

        public void ApplyDamage(DamageData damageData)
        {
            // CHG 공격은 비활성화된 어댑터도 찾을 수 있으므로 직접 검사한다.
            if (!isActiveAndEnabled ||
                _receiverComponent == null ||
                !_receiverComponent.isActiveAndEnabled ||
                _receiver == null)
            {
                return;
            }

            float damage = damageData.DamageAmount;

            // 잘못된 수치가 체력 계산으로 전달되지 않도록 막는다.
            if (damage <= 0f ||
                float.IsNaN(damage) ||
                float.IsInfinity(damage))
            {
                return;
            }

            _receiver.ReceiveDamage(damage);
        }
    }
}