using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 돌진 한 번의 명중만 관리한다. 이전 위치부터 현재 위치까지 검사해 고속 이동의 판정 누락을 줄인다.
    /// 피해와 밀치기는 인터페이스로 전달하므로 플레이어 이동/체력의 구체적인 구현을 알 필요가 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DashHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask _hitLayers = ~0;
        private readonly HashSet<IDamageReceiver> _damaged = new HashSet<IDamageReceiver>();
        private readonly HashSet<IKnockbackReceiver> _pushed = new HashSet<IKnockbackReceiver>();
        public bool IsActive { get; private set; }
        public int DamageCount => _damaged.Count;
        // 보스 대쉬는 플레이어만 공격한다. null은 독립 테스트/일반 공격에서 전체 수신자를 허용한다.
        public Transform TargetRoot { get; set; }

        public void Begin()
        {
            _damaged.Clear();
            _pushed.Clear();
            IsActive = true;
        }

        public void Sweep(Vector3 from, Vector3 to, Vector3 direction, float width, float height,
            float damage, float knockbackSpeed, float knockbackDuration)
        {
            if (!IsActive) return;
            Vector3 displacement = to - from;
            displacement.y = 0f;
            Vector3 center = (from + to) * 0.5f + Vector3.up * height * 0.5f;
            Vector3 halfSize = new Vector3(width * 0.5f, height * 0.5f,
                Mathf.Max(0.01f, displacement.magnitude * 0.5f));
            Collider[] colliders = Physics.OverlapBox(center, halfSize, Quaternion.LookRotation(direction),
                _hitLayers, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                if (!IsActive) break;
                if (collider.transform.IsChildOf(transform)) continue;
                if (TargetRoot != null && !collider.transform.IsChildOf(TargetRoot)) continue;
                IDamageReceiver receiver = collider.GetComponentInParent<IDamageReceiver>();
                // Collider가 여러 개인 캐릭터도 같은 피해 수신자에게 한 번만 피해를 준다.
                if (receiver != null && _damaged.Add(receiver)) receiver.ReceiveDamage(damage);
                if (!IsActive) break;
                IKnockbackReceiver knockback = collider.GetComponentInParent<IKnockbackReceiver>();
                if (knockback != null && _pushed.Add(knockback))
                    knockback.ReceiveKnockBack(direction, knockbackSpeed, knockbackDuration);
            }
        }

        // 기록은 다음 Begin까지 남겨 Inspector/테스트에서 마지막 결과를 확인할 수 있게 한다.
        public void Stop() => IsActive = false;
        private void OnDisable() => Stop();
    }
}
