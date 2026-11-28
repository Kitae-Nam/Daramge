using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Movement
{
    /// <summary>거신병의 회전은 공격 방향, 이동 방향, 플레이어 방향 순서로 결정한다.</summary>
    [DisallowMultipleComponent]
    public sealed class ChessColossusFacing : MonoBehaviour
    {
        private Transform _target;
        private BossFacing _previousFacing;
        private Vector3 _moveDirection;
        private bool _attackLocked;

        private void OnEnable()
        {
            // 씬에 기존 BossFacing을 붙인 거신병도 회전을 두 번 적용하지 않게 한다.
            if (TryGetComponent(out BossFacing facing) && facing.enabled)
            {
                _previousFacing = facing;
                facing.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (_previousFacing != null) _previousFacing.enabled = true;
            _previousFacing = null;
        }

        public void SetTarget(Transform target) => _target = target;
        public void SetMoveDirection(Vector3 direction) => _moveDirection = direction;
        public void ClearMoveDirection() => _moveDirection = Vector3.zero;

        public void LockAttack(Vector3 direction)
        {
            _attackLocked = true;
            Face(direction);
        }

        public void UnlockAttack() => _attackLocked = false;

        private void LateUpdate()
        {
            if (_attackLocked) return;
            if (_moveDirection.sqrMagnitude > .0001f) Face(_moveDirection);
            else if (_target != null) Face(_target.position - transform.position);
        }

        private void Face(Vector3 direction)
        {
            if (direction.x * direction.x + direction.z * direction.z < .0001f) return;

            // Unity 정면(+Z)을 0도로 두고 가장 가까운 45도 방향으로 맞춘다.
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, Mathf.Round(yaw / 45f) * 45f, 0f);
        }
    }
}
