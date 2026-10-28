using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 체스 킹 전용 추적. 칸 중심 사이를 한 칸씩 이동하고 잠시 멈춘다.
    /// NavMesh는 통과 가능 여부만 검증하며 자유 경로 추적은 사용하지 않는다. 대쉬는 별도 이동기다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChessBoardGrid), typeof(BossTarget), typeof(NavMeshAgent))]
    public sealed class ChessKingGridMovement : MonoBehaviour, IBossChaseMovement
    {
        [SerializeField, Min(0.05f)] private float _stepDuration = 0.35f;
        [SerializeField, Min(0f)] private float _stepPause = 0.2f;
        [SerializeField, Min(0f)] private float _stopDistance = 7f;
        [SerializeField] private LayerMask _obstacleLayers = Physics.DefaultRaycastLayers;
        private ChessBoardGrid _grid;
        private BossTarget _target;
        private NavMeshAgent _agent;
        private BossPatternRunner _runner;
        private BossCombatGate _gate;
        private Vector3 _start;
        private Vector3 _destination;
        private float _elapsed;
        private float _nextStepTime;
        private bool _chase = true;
        private bool _previousUpdatePosition;
        public bool IsStepping { get; private set; }
        public bool IsCentered
        {
            get
            {
                if (_grid == null) return true;
                Vector3 offset = _grid.CellToWorld(_grid.WorldToCell(transform.position)) - transform.position;
                offset.y = 0f;
                return offset.sqrMagnitude < 0.0025f;
            }
        }
        private bool Ready => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
        private void Awake()
        {
            _grid = GetComponent<ChessBoardGrid>(); _target = GetComponent<BossTarget>();
            _agent = GetComponent<NavMeshAgent>(); _runner = GetComponent<BossPatternRunner>(); _gate = GetComponent<BossCombatGate>();
            // 일반 추적과 그리드 이동이 같은 Agent를 동시에 조작하지 않게 한다.
            var normalMovement = GetComponent<BossMovement>();
            if (normalMovement != null) normalMovement.enabled = false;
        }
        private void Update()
        {
            if (!Ready) { StopStep(); return; }
            // 한 칸 이동 중에도 매 프레임 상태를 확인한다. 연결 오류가 있어도 그로기/공격 중에는 이동하지 않는다.
            bool blockedByAction = _runner != null &&
                (_runner.State != BossActionState.Idle || _runner.CurrentPattern != null);
            if (!_chase || (_gate != null && !_gate.IsOpen) || blockedByAction) { StopStep(); return; }
            if (IsStepping) { AdvanceStep(); return; }
            if (!_target.HasTarget || Time.time < _nextStepTime || (_runner != null && _runner.CurrentPattern != null)) return;
            _agent.isStopped = true;
            Vector2Int cell = _grid.WorldToCell(transform.position);
            Vector3 goal = _grid.CellToWorld(cell);
            if (!IsCentered)
            {
                // 대쉬/컷신이 칸 사이에서 끝났으면 순간이동하지 않고 가까운 칸 중심에 먼저 정렬한다.
                if (TryResolve(goal, out Vector3 aligned) && CanTravel(transform.position, aligned)) BeginStep(aligned);
                return;
            }
            Vector3 offset = _target.Target.position - transform.position; offset.y = 0f;
            if (offset.sqrMagnitude <= _stopDistance * _stopDistance) return;
            if (_grid.TryFindKingStep(cell, _grid.WorldToCell(_target.Target.position), CanTraverse, out Vector2Int next) &&
                TryResolve(_grid.CellToWorld(next), out Vector3 destination)) BeginStep(destination);
        }
        private bool CanTraverse(Vector2Int from, Vector2Int to)
            => TryResolve(_grid.CellToWorld(from), out Vector3 start) && TryResolve(_grid.CellToWorld(to), out Vector3 end) && CanTravel(start, end);
        private bool TryResolve(Vector3 point, out Vector3 result)
        {
            point.y = transform.position.y;
            bool found = NavMesh.SamplePosition(point, out NavMeshHit hit, 0.2f, _agent.areaMask);
            result = hit.position;
            Vector3 offset = result - point; offset.y = 0f;
            return found && offset.sqrMagnitude < 0.01f;
        }
        private bool CanTravel(Vector3 from, Vector3 to)
        {
            if (NavMesh.Raycast(from, to, out _, _agent.areaMask)) return false;
            Vector3 delta = to - from; delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f) return true;
            Vector3 half = new Vector3(_agent.radius, Mathf.Max(0.05f, _agent.height * 0.5f - 0.1f), 0.01f);
            RaycastHit[] hits = Physics.BoxCastAll(from + Vector3.up * (_agent.height * 0.5f), half, delta.normalized,
                Quaternion.LookRotation(delta), delta.magnitude, _obstacleLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<IDamageReceiver>() != null ||
                    hit.collider.GetComponentInParent<IKnockbackReceiver>() != null) continue;
                return false;
            }
            return true;
        }
        private void BeginStep(Vector3 destination)
        {
            _start = transform.position; _destination = destination; _elapsed = 0f;
            _agent.ResetPath(); _agent.velocity = Vector3.zero; _agent.isStopped = true;
            _previousUpdatePosition = _agent.updatePosition;
            _agent.updatePosition = false;
            IsStepping = true;
        }
        private void AdvanceStep()
        {
            _elapsed += Time.deltaTime;
            Vector3 next = Vector3.Lerp(_start, _destination, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / _stepDuration)));
            if (!CanTravel(transform.position, next)) { StopStep(); return; }
            _agent.Move(next - transform.position);
            transform.position = _agent.nextPosition;
            if (_elapsed >= _stepDuration || (transform.position - next).sqrMagnitude > 0.04f) StopStep();
        }
        private void StopStep()
        {
            if (!IsStepping) return;
            IsStepping = false;
            if (_agent != null)
            {
                if (Ready) { _agent.ResetPath(); _agent.velocity = Vector3.zero; _agent.isStopped = true; }
                _agent.updatePosition = _previousUpdatePosition;
            }
            _nextStepTime = Time.time + _stepPause;
        }
        public void SetChaseEnabled(bool enabled)
        {
            _chase = enabled;
            if (enabled) return;
            StopStep();
            // 아직 한 칸 이동을 시작하지 않았어도 컷신/페이즈 종료는 Agent 자체를 정지시킨다.
            // ResetPath/velocity 설정 뒤 마지막에 isStopped를 기록해 수동 이동 상태를 확실히 닫는다.
            if (Ready) { _agent.ResetPath(); _agent.velocity = Vector3.zero; _agent.isStopped = true; }
        }
        private void OnDisable() => StopStep();
    }
}
