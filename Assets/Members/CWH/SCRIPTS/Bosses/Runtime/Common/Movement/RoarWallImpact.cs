using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement
{
    /// <summary>
    /// 포효로 밀리는 동안 실제 벽 접촉이 발생했을 때만 한 번 피해를 준다.
    /// 이동 자체는 기존 IKnockbackReceiver가 맡아 플레이어 이동 구현과 결합하지 않는다.
    /// </summary>
    public sealed class RoarWallImpact : MonoBehaviour
    {
        private float _armedUntil;
        private float _damage;
        private Vector3 _direction;
        private LayerMask _wallLayers;
        private IDamageReceiver _receiver;
        private BossCombatGate _gate;
        private BossHealth _owner;

        public void Arm(Transform owner, Vector3 direction, float duration, float damage, LayerMask walls)
        {
            _armedUntil = Time.time + duration;
            _direction = direction.normalized;
            _damage = damage;
            _wallLayers = walls;
            _receiver = GetComponentInParent<IDamageReceiver>();
            _gate = owner.GetComponent<BossCombatGate>();
            _owner = owner.GetComponent<BossHealth>();
        }

        public bool TryApplyWallImpact(Collider wall, Vector3 normal)
        {
            if (Time.time >= _armedUntil || _receiver == null || _owner == null || _owner.IsDead ||
                (_gate != null && !_gate.IsOpen) || wall == null) return false;
            if ((_wallLayers.value & (1 << wall.gameObject.layer)) == 0 || wall.isTrigger ||
                wall.transform.IsChildOf(transform) || wall.transform.IsChildOf(_owner.transform) ||
                wall.GetComponentInParent<IDamageReceiver>() != null) return false;
            // 바닥 접촉과 밀리는 방향 뒤쪽의 접촉은 벽 충돌 보상으로 취급하지 않는다.
            if (Mathf.Abs(normal.y) > 0.6f || Vector3.Dot(normal, _direction) >= -0.2f) return false;
            _armedUntil = 0f;
            _receiver.ReceiveDamage(_damage);
            return true;
        }

        private void OnCollisionEnter(Collision collision) => CheckCollision(collision);
        private void OnCollisionStay(Collision collision) => CheckCollision(collision);
        private void CheckCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
                if (TryApplyWallImpact(collision.collider, collision.GetContact(i).normal)) break;
        }
        private void OnControllerColliderHit(ControllerColliderHit hit) => TryApplyWallImpact(hit.collider, hit.normal);
        private void OnDisable() => _armedUntil = 0f;
    }
}
