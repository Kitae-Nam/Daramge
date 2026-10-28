using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>경고와 실제 이동이 공유하는, 월드 좌표 기준의 고정된 직선 경로다.</summary>
    public readonly struct DashPath
    {
        public Vector3 Start { get; }
        public Vector3 Direction { get; }
        public float Distance { get; }
        public Vector3 End => Start + Direction * Distance;

        public DashPath(Vector3 start, Vector3 direction, float distance)
        {
            direction.y = 0f;
            Start = start;
            Direction = direction.normalized;
            Distance = Mathf.Max(0f, distance);
        }
    }

    /// <summary>
    /// 길 찾기와 구분된 직선 돌진 이동만 담당한다.
    /// 패턴은 목적지를 계속 추적하지 않고, 경고 시작 때 만든 DashPath를 끝까지 사용한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class BossDashMotor : MonoBehaviour
    {
        [SerializeField] private LayerMask _obstacleLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float _wallPadding = 0.1f;
        [SerializeField, Min(0.01f)] private float _bodyHalfDepth = 0.1f;
        private NavMeshAgent _agent;
        private bool _previousUpdatePosition;
        private bool _previousUpdateRotation;
        private bool _previousStopped;

        private NavMeshAgent Agent => _agent != null ? _agent : (_agent = GetComponent<NavMeshAgent>());
        public bool IsReady => Agent != null && Agent.isActiveAndEnabled && Agent.isOnNavMesh;
        public bool IsDashing { get; private set; }
        public float BodyHeight => Agent != null ? Mathf.Max(0.2f, Agent.height) : 2f;

        /// <summary>NavMesh 경계와 물리 장애물 중 먼저 만나는 지점까지 경고 경로를 줄인다.</summary>
        public bool TryPlan(Vector3 targetPosition, float distance, float width, out DashPath path)
        {
            path = default;
            if (!IsReady || distance <= 0f || width <= 0f) return false;
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return false;

            direction.Normalize();
            float clearDistance = GetClearDistance(transform.position, direction, distance, width);
            path = new DashPath(transform.position, direction, clearDistance);
            return clearDistance > 0.05f;
        }

        public bool Begin(DashPath path)
        {
            if (IsDashing || !IsReady) return false;
            Vector3 offset = transform.position - path.Start;
            offset.y = 0f;
            // 준비 중 외부 연출이 보스를 옮겼다면 예고와 다른 곳에서 돌진하지 않는다.
            if (offset.sqrMagnitude > 0.01f) return false;

            _previousUpdatePosition = Agent.updatePosition;
            _previousUpdateRotation = Agent.updateRotation;
            _previousStopped = Agent.isStopped;
            Agent.isStopped = true;
            Agent.ResetPath();
            Agent.velocity = Vector3.zero;
            // 자동 위치 갱신과 직접 이동이 경쟁하지 않게 돌진 동안만 위치 갱신을 맡는다.
            Agent.updatePosition = false;
            Agent.updateRotation = false;
            transform.rotation = Quaternion.LookRotation(path.Direction);
            IsDashing = true;
            return true;
        }

        /// <summary>한 프레임의 이동량만 처리한다. 경고 후 생긴 장애물도 다시 검사한다.</summary>
        public bool Step(Vector3 direction, float requestedDistance, float width)
        {
            if (!IsDashing || !IsReady || requestedDistance <= 0f) return false;
            Vector3 before = transform.position;
            float allowed = GetClearDistance(before, direction, requestedDistance, width);
            if (allowed <= 0.0001f) return false;

            Agent.Move(direction * allowed);
            transform.position = Agent.nextPosition;
            Vector3 actual = transform.position - before;
            actual.y = 0f;
            // 벽에 걸리거나 옆으로 밀리면 종료해 예고한 직선 밖으로 계속 이동하지 않는다.
            float forward = Vector3.Dot(actual, direction);
            Vector3 sideways = actual - direction * forward;
            return allowed >= requestedDistance - 0.001f && forward >= requestedDistance - 0.01f
                                                         && sideways.sqrMagnitude < 0.0025f;
        }

        private float GetClearDistance(Vector3 start, Vector3 direction, float distance, float width)
        {
            float result = distance;
            if (Agent.Raycast(start + direction * distance, out NavMeshHit navHit))
                result = Mathf.Max(0f, Vector3.Dot(navHit.position - start, direction) - _wallPadding);

            // 바닥과 겹치지 않도록 몸체 하단에 여유를 둔 얇은 박스를 직선으로 쓸어 검사한다.
            Vector3 center = start + Vector3.up * (BodyHeight * 0.5f);
            Vector3 halfSize = new Vector3(width * 0.5f, BodyHeight * 0.5f - 0.05f, _bodyHalfDepth);
            RaycastHit[] hits = Physics.BoxCastAll(center, halfSize, direction,
                Quaternion.LookRotation(direction), distance, _obstacleLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                // 피해를 받는 대상은 벽으로 취급하지 않는다. 명중 처리는 별도 컴포넌트가 담당한다.
                if (hit.collider.GetComponentInParent<IDamageReceiver>() != null ||
                    hit.collider.GetComponentInParent<IKnockbackReceiver>() != null) continue;
                result = Mathf.Min(result, Mathf.Max(0f, hit.distance - _wallPadding));
            }

            return result;
        }

        /// <summary>완료/그로기/사망/비활성화에서 공통 호출하며 여러 번 호출해도 안전하다.</summary>
        public void Stop()
        {
            if (!IsDashing) return;
            IsDashing = false;
            if (Agent == null) return;
            if (IsReady)
            {
                Agent.velocity = Vector3.zero;
                Agent.ResetPath();
                Agent.isStopped = _previousStopped;
            }

            Agent.updatePosition = _previousUpdatePosition;
            Agent.updateRotation = _previousUpdateRotation;
        }

        private void OnDisable() => Stop();
    }
}