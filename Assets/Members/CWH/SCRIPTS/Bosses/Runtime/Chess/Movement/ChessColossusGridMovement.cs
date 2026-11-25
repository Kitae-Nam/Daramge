using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>공용 그리드의 기물 경로를 실행하고, NavMesh와 장애물로 실제 통과 여부를 확인한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class ChessColossusGridMovement : ChessMinionMovement
    {
        private static readonly int Moving = Animator.StringToHash("Moving");
        private static readonly int DragPhase = Animator.StringToHash("DragPhase");
        private static readonly int DragStride = Animator.StringToHash("DragStride");
        [SerializeField] private ChessBoardGrid _board;
        [SerializeField] private Transform _target;
        [SerializeField] private Animator _animator;
        [SerializeField, Range(1, 7)] private int _maxSlideCells = 3;
        [SerializeField, Min(0.1f)] private float _speed = 2.8f;
        [SerializeField, Min(0f)] private float _stepPause = 0.18f;
        [SerializeField, Min(0.1f)] private float _standaloneStoppingDistance = 2.5f;
        [SerializeField] private LayerMask _obstacleLayers = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] _obstacles = new RaycastHit[24];
        private NavMeshAgent _agent;
        private Transform _summoner;
        private bool _externallyDriven;
        private bool _moving;
        private bool _wasUpdatingPosition;
        private bool _wasUpdatingRotation;
        private bool _hasSecondLeg;
        private bool _isAlignment;
        private ChessColossusMoveKind _preferred;
        private ChessColossusMoveKind _activeKind;
        private Vector3 _start;
        private Vector3 _destination;
        private Vector3 _secondDestination;
        private float _elapsed;
        private float _duration;
        private int _pullCount;
        private float _retry;

        private bool Ready => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
        private NavMeshQueryFilter Filter => new NavMeshQueryFilter { areaMask = _agent.areaMask, agentTypeID = _agent.agentTypeID };

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            // 테스트 배치에서는 Inspector의 보드/대상을, 소환 시에는 ChessMinion의 상태 머신을 사용한다.
            if (!_externallyDriven) Advance(_standaloneStoppingDistance, Time.deltaTime);
        }

        public override void Bind(ChessBoardGrid board, Transform target, Transform summoner)
        {
            Stop();
            _externallyDriven = true;
            if (board != null) _board = board;
            _target = target;
            _summoner = summoner;
            _retry = 0f;
            _preferred = ChessColossusMoveKind.Rook;
        }

        public override bool Advance(float stoppingDistance, float deltaTime)
        {
            if (!Ready || _board == null || _target == null) { Stop(); return false; }
            if (_moving) { AdvanceLeg(deltaTime); return false; }

            Vector2Int from = _board.WorldToCell(transform.position);
            bool centered = HorizontalDistanceSquared(transform.position, _board.CellToWorld(from)) < 0.0025f;
            float rangeSquared = stoppingDistance * stoppingDistance;
            if (centered && HorizontalDistanceSquared(transform.position, _target.position) <= rangeSquared) return true;
            _retry -= deltaTime;
            if (_retry > 0f) return false;
            if (centered) PlanMove(from, rangeSquared);
            else AlignToGrid();
            if (!_moving) _retry = Mathf.Max(0.1f, _stepPause);
            return false;
        }

        private void AlignToGrid()
        {
            bool CanAlign(Vector2Int cell) => !Occupied(cell) && TryResolve(cell, out Vector3 point) && CanTravel(transform.position, point);
            if (_board.TryFindNearestUsableCell(transform.position, CanAlign, out Vector2Int nearest) && TryResolve(nearest, out Vector3 destination))
                BeginMove(destination, destination, alignment: true);
        }

        private void PlanMove(Vector2Int from, float rangeSquared)
        {
            Vector2Int target = _board.WorldToCell(_target.position);
            var traversable = new Dictionary<(Vector2Int, Vector2Int), bool>();
            bool CanTraverse(Vector2Int start, Vector2Int end)
            {
                var key = (start, end);
                if (traversable.TryGetValue(key, out bool result)) return result;
                result = !Occupied(end) && TryResolve(start, out Vector3 startPoint) &&
                    TryResolve(end, out Vector3 endPoint) && CanTravel(startPoint, endPoint);
                traversable.Add(key, result);
                return result;
            }

            bool InRange(Vector2Int cell) => HorizontalDistanceSquared(_board.CellToWorld(cell), _target.position) <= rangeSquared;
            if (!ChessColossusMovePlanner.TryPlan(_board, from, target, InRange, CanTraverse, _maxSlideCells, _preferred, out var move) ||
                !TryResolve(move.FirstWaypoint, out Vector3 first) || !TryResolve(move.Destination, out Vector3 last)) return;
            _activeKind = move.Kind;
            BeginMove(first, last, alignment: false);
        }

        private void BeginMove(Vector3 first, Vector3 last, bool alignment)
        {
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
            _agent.isStopped = true;
            _wasUpdatingPosition = _agent.updatePosition;
            _wasUpdatingRotation = _agent.updateRotation;
            // Agent가 자유 추적을 하는 대신 지정한 선분만 따라가도록 제어권을 빌린다.
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _moving = true;
            _isAlignment = alignment;
            _hasSecondLeg = first != last;
            _secondDestination = last;
            StartLeg(first);
            SetMoving(true);
        }

        private void StartLeg(Vector3 destination)
        {
            _start = transform.position;
            _destination = destination;
            _elapsed = 0f;
            float distance = Mathf.Sqrt(HorizontalDistanceSquared(_start, destination));
            _pullCount = Mathf.Max(1, Mathf.CeilToInt(distance / ChessColossusDragGait.MaxStride));
            _duration = Mathf.Max(ChessColossusDragGait.MinCycleDuration * _pullCount, distance / _speed);
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetFloat(DragPhase, 0f);
                _animator.SetFloat(DragStride, distance / _pullCount);
            }
        }

        private void AdvanceLeg(float deltaTime)
        {
            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            Vector3 current = transform.position;
            // 주먹을 내미는 동안은 제자리에 있고, 두 주먹이 닿았을 때만 본체를 움직인다.
            float dragged = ChessColossusDragGait.LegProgress(progress, _pullCount, out float phase);
            Vector3 next = Vector3.Lerp(_start, _destination, dragged);
            if (!CanTravel(current, next)) { Stop(); return; }

            Vector3 heading = _destination - _start;
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading), 540f * deltaTime);
            if (_animator != null && _animator.runtimeAnimatorController != null) _animator.SetFloat(DragPhase, phase);
            _agent.Move(next - current);
            transform.position = _agent.nextPosition;
            if (HorizontalDistanceSquared(transform.position, next) > 0.01f) { Stop(); return; }
            if (progress < 1f) return;
            if (_hasSecondLeg)
            {
                _hasSecondLeg = false;
                StartLeg(_secondDestination);
                return;
            }

            if (!_isAlignment) _preferred = (ChessColossusMoveKind)(((int)_activeKind + 1) % 3);
            Stop();
        }

        private bool Occupied(Vector2Int cell) => cell == _board.WorldToCell(_target.position) ||
            (_summoner != null && cell == _board.WorldToCell(_summoner.position));

        private bool TryResolve(Vector2Int cell, out Vector3 destination)
        {
            Vector3 point = _board.CellToWorld(cell);
            point.y = transform.position.y;
            if (NavMesh.SamplePosition(point, out NavMeshHit sample, 0.25f, Filter) &&
                HorizontalDistanceSquared(point, sample.position) < 0.0025f)
            {
                // NavMesh로 높이만 보정해 모든 목적지가 그리드의 정확한 중심에 놓이게 한다.
                destination = new Vector3(point.x, sample.position.y, point.z);
                return true;
            }
            destination = default;
            return false;
        }

        private bool CanTravel(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance < 0.0001f) return true;
            if (NavMesh.Raycast(from, to, out _, Filter)) return false;
            direction /= distance;
            var ray = new Ray(from, direction);
            Vector2Int source = _board.WorldToCell(from);
            if (CrossesOccupiedCell(ray, distance, source, _target) || CrossesOccupiedCell(ray, distance, source, _summoner)) return false;
            Vector3 half = new Vector3(_agent.radius, Mathf.Max(0.05f, _agent.height * 0.5f - 0.1f), 0.01f);
            int count = Physics.BoxCastNonAlloc(from + Vector3.up * (_agent.height * 0.5f), half, direction, _obstacles,
                Quaternion.LookRotation(direction), distance, _obstacleLayers, QueryTriggerInteraction.Ignore);
            if (count == _obstacles.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform obstacle = _obstacles[i].transform;
                if (IsUnder(obstacle, transform) || IsUnder(obstacle, _target) || IsUnder(obstacle, _summoner)) continue;
                return false;
            }
            return true;
        }

        private bool CrossesOccupiedCell(Ray ray, float distance, Vector2Int source, Transform occupant)
        {
            if (occupant == null) return false;
            Vector2Int cell = _board.WorldToCell(occupant.position);
            // 같은 칸에서 벗어날 수는 있다. 이동 중 새로 점유된 칸은 프레임이 길어도 통과하지 않는다.
            if (cell == source) return false;
            Vector3 center = _board.CellToWorld(cell);
            center.y = ray.origin.y;
            float width = _board.CellSize - 0.002f;
            var bounds = new Bounds(center, new Vector3(width, 1f, width));
            return bounds.IntersectRay(ray, out float entry) && entry <= distance;
        }

        private static bool IsUnder(Transform item, Transform root) => root != null && item.IsChildOf(root);

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        public override void Stop()
        {
            if (!_moving) return;
            _moving = false;
            _hasSecondLeg = false;
            if (Ready)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
                _agent.isStopped = true;
            }
            _agent.updatePosition = _wasUpdatingPosition;
            _agent.updateRotation = _wasUpdatingRotation;
            _retry = _stepPause;
            SetMoving(false);
        }

        private void SetMoving(bool moving)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null) _animator.SetBool(Moving, moving);
        }

        private void OnDisable() => Stop();
    }
}
