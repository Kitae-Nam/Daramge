using System.Collections.Generic;
using System.Linq;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Presentation;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons
{
    /// <summary>거신병의 공격 선택, 예고 고정, 애니메이션에 맞춘 1회 판정을 조율한다.</summary>
    [DisallowMultipleComponent]
    public sealed class ChessColossusAttack : ChessMinionAttack
    {
        public const float SlamImpactTime = 44f / 60f;
        // 60fps 클립의 도약, 정점, 양손 접지 키에서 루트 궤적과 판정을 맞춘다.
        public const float LeapTakeoffTime = 14f / 60f;
        public const float LeapApexTime = 30f / 60f;
        public const float LeapImpactTime = 43f / 60f;
        public const float SweepImpactTime = .6f;
        public const float ClipDuration = 1.2f;
        private const float SweepTraceStart = .51f;
        private const float SweepTraceEnd = .70f;
        private const float RookRiseDuration = .32f;
        private const float RookRowDuration = .075f;
        private const float RookLaunchLead = RookRowDuration / 3f;

        public enum Move { Slam, Leap, Sweep }

        private struct RookLane
        {
            public float Distance;
            public Vector3 Contact;
            public Vector3 Normal;
            public bool Finished;
        }

        [SerializeField] private Animator _animator;
        [SerializeField, Min(1)] private int _jumpRangeCells = 3;
        [SerializeField, Range(2, 3)] private int _sweepWidth = 3;
        [SerializeField] private LayerMask _rookWallLayers = 1 << 10;
        [SerializeField, Min(0.1f)] private float _jumpHeight = 1.48f;
        [SerializeField, Min(0f)] private float _sweepKnockbackSpeed = 15f;
        [SerializeField, Min(0f)] private float _sweepKnockbackDuration = .34f;
        [SerializeField, Range(0f, .15f)] private float _sweepHitPause = .055f;
        [SerializeField] private ChessSweepPresentation _sweepPresentation;
        [SerializeField] private ChessColossusImpactFeedback _impactFeedback;
        [SerializeField] private ChessColossusGridEffects _gridEffects;
        private static readonly int[] States = { Animator.StringToHash("Base Layer.Slam"), Animator.StringToHash("Base Layer.LeapSlam"), Animator.StringToHash("Base Layer.Sweep") };
        private ChessBoardGrid _board;
        private Transform _summoner;
        private Move _next;
        private Move _selected;
        private Vector3 _origin;
        private Vector3 _start;
        private Vector3 _facing;
        private Vector3 _bodyForward;
        private List<Vector3> _primary;
        private readonly List<List<Vector3>> _followUp = new List<List<Vector3>>();
        private readonly RookLane[] _rookLanes = new RookLane[2];
        private readonly List<Vector3> _rookDamageCells = new List<Vector3>(2);
        private ChessColossusAttackPattern.JumpPiece _wave;
        private int _nextWave;
        private int _followUpIndex;
        private int _raisedCount;
        private bool _hit;
        private float _elapsed;
        private float _impactTime;
        private float _hitPauseRemaining;
        private float _animatorSpeedBeforePause;
        private bool _traceStarted;
        private bool _prepared;
        private bool _attacking;
        private bool _impacted;
        private bool _airborne;
        private bool _updatePosition;
        private bool _updateRotation;
        private bool _closeOnly;
        private BossFacing _pausedFacing;
        private ChessColossusFacing _colossusFacing;

        private float CellSize => _board != null ? _board.CellSize : 1.5f;
        private float JumpRange => CellSize * _jumpRangeCells;
        public override float TriggerDistance => _closeOnly ? CellSize * 1.45f : JumpRange + CellSize;
        private bool OnNavMesh => Agent != null && Agent.isActiveAndEnabled && Agent.isOnNavMesh;

        private void Awake()
        {
            _colossusFacing = GetComponent<ChessColossusFacing>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_sweepPresentation == null) _sweepPresentation = GetComponent<ChessSweepPresentation>();
            if (_impactFeedback == null) _impactFeedback = GetComponent<ChessColossusImpactFeedback>();
            if (_gridEffects == null) _gridEffects = GetComponent<ChessColossusGridEffects>();
        }

        public override void BindBoard(ChessBoardGrid board, Transform summoner)
        {
            _board = board;
            _summoner = summoner;
            _closeOnly = false;
            _next = Move.Slam;
            _nextWave = 0;
        }

        protected override bool TryPrepare()
        {
            _prepared = false;
            if (_board == null) return false;
            Vector3 direction = Flat(Target.position - transform.position);
            float distance = direction.magnitude;
            direction = distance > .001f ? direction / distance : transform.forward;
            for (int offset = 0; offset < States.Length; offset++)
            {
                var move = (Move)(((int)_next + offset) % States.Length);
                if (!TryShape(move, direction, distance)) continue;
                _selected = move;
                _prepared = true;
                _closeOnly = false;
                return true;
            }
            _closeOnly = true;
            return false;
        }

        private bool TryShape(Move move, Vector3 direction, float distance)
        {
            _followUp.Clear();
            Vector2Int forward = ChessColossusAttackPattern.Facing(direction);
            _facing = _bodyForward = new Vector3(forward.x, 0f, forward.y);
            Vector2Int start = _board.WorldToCell(transform.position);
            Vector2Int target = _board.WorldToCell(Target.position);
            List<Vector2Int> primary;
            _origin = transform.position;
            switch (move)
            {
                case Move.Slam:
                    primary = ChessColossusAttackPattern.Rectangle(_board, start, forward, 4, 2, 1);
                    if (!primary.Contains(target)) return false;
                    for (int row = 1; row <= 6; row++) AddFollowUp(ChessColossusAttackPattern.Rectangle(_board, start, forward, 2, 1, row));
                    PrepareRookLanes();
                    _impactTime = SlamImpactTime;
                    break;
                case Move.Leap:
                    if (distance > TriggerDistance || !TryFindLanding(out _origin)) return false;
                    Vector2Int landed = _board.WorldToCell(_origin);
                    primary = ChessColossusAttackPattern.Landing(_board, landed);
                    _wave = (ChessColossusAttackPattern.JumpPiece)_nextWave;
                    for (int ring = 2; ring <= 3; ring++) AddFollowUp(ChessColossusAttackPattern.JumpFollowUp(_board, landed, _wave, ring));
                    _impactTime = LeapImpactTime;
                    break;
                default:
                    primary = ChessColossusAttackPattern.Rectangle(_board, start, forward, _sweepWidth, 1, 1);
                    if (!primary.Contains(target)) return false;
                    _impactTime = SweepImpactTime;
                    break;
            }
            _primary = WorldCells(primary);
            return _primary.Count > 0;
        }

        private void AddFollowUp(List<Vector2Int> cells) { if (cells.Count > 0) _followUp.Add(WorldCells(cells)); }

        private void PrepareRookLanes()
        {
            if (_followUp.Count == 0) return;
            float range = (_followUp.Count - 1) * CellSize;
            // Wall 레이어만 검사하고, 경고를 그리기 전에 각 룩의 끝점을 확정한다.
            for (int lane = 0; lane < _followUp[0].Count; lane++)
            {
                Vector3 start = _followUp[0][lane];
                start.y = _board.SurfaceHeight + CellSize * .35f;
                _rookLanes[lane] = range > 0f &&
                    Physics.SphereCast(start, CellSize * .2f, _facing, out RaycastHit wall,
                        range, _rookWallLayers, QueryTriggerInteraction.Ignore)
                    ? new RookLane { Distance = wall.distance, Contact = wall.point, Normal = wall.normal }
                    : new RookLane { Distance = range, Contact = start + _facing * range, Normal = -_facing };
            }
        }

        private bool RookReachesRow(int lane, int row) => row * CellSize <= _rookLanes[lane].Distance + .001f;

        private List<Vector3> WorldCells(List<Vector2Int> cells) => cells.Select(cell =>
        {
            Vector3 point = _board.CellToWorld(cell);
            point.y = _origin.y;
            return point;
        }).ToList();

        private bool TryFindLanding(out Vector3 destination)
        {
            destination = default;
            if (!OnNavMesh) return false;
            var filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
            Vector3 start = transform.position;
            Vector3 target = Target.position;
            Vector3 toward = Flat(target - start);
            Vector3 desired = start + toward.normalized * Mathf.Clamp(toward.magnitude - CellSize, 0f, JumpRange);

            Vector2Int playerCell = _board.WorldToCell(target);
            Vector2Int bossCell = _summoner != null ? _board.WorldToCell(_summoner.position) : new Vector2Int(-1, -1);
            bool CanLand(Vector2Int cell) => cell != playerCell && cell != bossCell && Resolve(cell, out _);
            return _board.TryFindNearestUsableCell(desired, CanLand, out Vector2Int safe) && Resolve(safe, out destination);

            bool Resolve(Vector2Int cell, out Vector3 landing)
            {
                landing = default;
                Vector3 center = _board.CellToWorld(cell);
                center.y = start.y;
                if (!NavMesh.SamplePosition(center, out NavMeshHit sample, .25f, filter) || Flat(sample.position - center).sqrMagnitude > .0025f)
                    return false;
                center.y = sample.position.y;
                return AcceptLanding(center, out landing);
            }

            bool AcceptLanding(Vector3 point, out Vector3 landing)
            {
                landing = point;
                int dx = Mathf.Abs(_board.WorldToCell(point).x - playerCell.x);
                int dz = Mathf.Abs(_board.WorldToCell(point).y - playerCell.y);
                return Flat(point - start).sqrMagnitude <= JumpRange * JumpRange + .001f && dx <= 1 && dz <= 1 &&
                    !NavMesh.Raycast(start, point, out _, filter);
            }
        }

        public override void ShowWarning()
        {
            if (!_prepared) return;
            LockAttackFacing();
            var followUp = _selected == Move.Slam
                ? _followUp.SelectMany((cells, row) => cells.Where((cell, lane) => RookReachesRow(lane, row)))
                : _followUp.SelectMany(cells => cells).Distinct();
            _gridEffects?.Show(_primary, followUp.ToArray(), CellSize, _board.SurfaceHeight);
        }

        public override void SetWarningProgress(float progress) => _gridEffects?.SetProgress(progress);

        public override void BeginAttack()
        {
            if (!_prepared) return;
            LockAttackFacing();
            _start = transform.position;
            _elapsed = 0f;
            _impacted = false;
            _traceStarted = false;
            _followUpIndex = 0;
            _raisedCount = 0;
            _hit = false;
            _attacking = true;
            _next = (Move)(((int)_selected + 1) % States.Length);
            if (_selected == Move.Leap) _nextWave = (_nextWave + 1) % 3;
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.Play(States[(int)_selected], 0, 0f);
            if (_selected == Move.Slam && _followUp.Count > 0)
                _gridEffects?.BeginRookCharge(_followUp[0], CellSize, _board.SurfaceHeight);
        }

        public override bool Tick(float deltaTime)
        {
            if (!_attacking) return true;
            if (_hitPauseRemaining > 0f)
            {
                float paused = Mathf.Min(deltaTime, _hitPauseRemaining);
                deltaTime -= paused;
                _hitPauseRemaining -= paused;
                if (_hitPauseRemaining > 0f) return false;
                RestoreAnimatorSpeed();
            }
            _elapsed += deltaTime;
            if (_selected == Move.Leap && !_impacted && _elapsed >= LeapTakeoffTime && !AdvanceLeap())
            {
                Cancel();
                return true;
            }
            if (!_impacted && _elapsed >= _impactTime)
            {
                _impacted = true;
                _gridEffects?.HidePrimary();
                ApplyImpact(_primary, true);
            }
            bool followUpFinished = _selected == Move.Slam ? TickRookCharge() : TickPieceRise();
            if (_selected == Move.Sweep) UpdateSweepTrace();
            if (_elapsed < ClipDuration || !followUpFinished) return false;
            _attacking = _prepared = false;
            RestoreFacing();
            return true;
        }

        private void LockAttackFacing()
        {
            // 경고와 판정에 쓰는 방향을 추적 회전이 덮어쓰지 않도록 공격 동안만 넘겨받는다.
            if (_pausedFacing == null && TryGetComponent(out BossFacing facing) && facing.enabled)
            {
                _pausedFacing = facing;
                facing.enabled = false;
            }
            if (_colossusFacing != null && _colossusFacing.isActiveAndEnabled)
                _colossusFacing.LockAttack(_bodyForward);
            else
                transform.rotation = Quaternion.LookRotation(_bodyForward);
        }

        private void RestoreFacing()
        {
            _colossusFacing?.UnlockAttack();
            // 원래 켜져 있어서 이 공격이 멈춘 컴포넌트만 복구한다.
            if (_pausedFacing != null) _pausedFacing.enabled = true;
            _pausedFacing = null;
        }

        private bool TickRookCharge()
        {
            if (_followUp.Count == 0) return true;
            var starts = _followUp[0];
            // 돌진 속도가 달라져도 주먹이 닿을 때 룩이 첫 칸에서 1/3칸 전진해 있도록 맞춘다.
            float time = _elapsed - (_impactTime - RookLaunchLead);
            float distance = Mathf.Max(0f, time / RookRowDuration) * CellSize;
            float riseProgress = _elapsed / RookRiseDuration;
            bool finished = true;
            for (int lane = 0; lane < starts.Count; lane++)
            {
                ref RookLane rook = ref _rookLanes[lane];
                if (rook.Finished) continue;
                float travelled = Mathf.Min(distance, rook.Distance);
                _gridEffects?.MoveChargingRook(lane, starts[lane], _facing * travelled, riseProgress);
                // 룩의 앞부분이 벽에 닿거나 마지막 칸에 닿는 즉시 충돌 연출을 한 번만 실행한다.
                rook.Finished = time >= RookLaunchLead && distance >= rook.Distance;
                if (rook.Finished) _gridEffects?.CrashChargingRook(lane, rook.Contact, rook.Normal, _facing);
                finished &= rook.Finished;
            }

            // 같은 도착 시간을 사용하되, 벽에 막힌 룩의 경로 뒤에는 피해를 적용하지 않는다.
            while (_impacted && _followUpIndex < _followUp.Count && time >= _followUpIndex * RookRowDuration)
            {
                int row = _followUpIndex++;
                var cells = _followUp[row];
                _rookDamageCells.Clear();
                for (int lane = 0; lane < cells.Count; lane++)
                    if (RookReachesRow(lane, row)) _rookDamageCells.Add(cells[lane]);
                if (_rookDamageCells.Count > 0) ApplyImpact(_rookDamageCells, false);
            }

            if (finished) _gridEffects?.HideWarnings();
            return finished;
        }

        private bool TickPieceRise()
        {
            while (_raisedCount < _followUp.Count && _elapsed >= FollowUpTime(_raisedCount))
            {
                List<Vector3> cells = _followUp[_raisedCount++];
                var piece = (ChessColossusGridEffects.Piece)((int)ChessColossusGridEffects.Piece.Rook + (int)_wave);
                _gridEffects?.Strike(cells, CellSize, piece, _board.SurfaceHeight);
            }
            while (_followUpIndex < _followUp.Count && _elapsed >= FollowUpTime(_followUpIndex) + .075f)
            {
                List<Vector3> cells = _followUp[_followUpIndex++];
                ApplyImpact(cells, false);
            }
            bool finished = _followUpIndex == _followUp.Count;
            if (_impacted && finished) _gridEffects?.HideWarnings();
            return finished;
        }

        private float FollowUpTime(int index) => _impactTime + .16f + index * .095f;

        private void UpdateSweepTrace()
        {
            if (_sweepPresentation == null || _elapsed < SweepTraceStart) return;
            EnsureSweepTrace();
            _sweepPresentation.Advance(Mathf.InverseLerp(SweepTraceStart, SweepTraceEnd, _elapsed));
            if (_elapsed >= SweepTraceEnd) _sweepPresentation.End();
        }

        private void ApplyImpact(List<Vector3> cells, bool primary)
        {
            bool sweep = _selected == Move.Sweep;
            if (sweep) EnsureSweepTrace();
            Vector3 contact = _origin;
            bool hit = !_hit && BossAttackQuery.TryApplyConfirmedCells(Target, cells, CellSize, Damage, _origin, _facing,
                sweep ? _sweepKnockbackSpeed : 0f, sweep ? _sweepKnockbackDuration : 0f, out contact);
            _hit |= hit;
            if (primary)
            {
                float surfaceHeight = _board.SurfaceHeight;
                _gridEffects?.Strike(cells, CellSize, ChessColossusGridEffects.Piece.None, surfaceHeight);
                Vector3 ground = _selected == Move.Leap ? _origin : cells.Aggregate(Vector3.zero, (sum, cell) => sum + cell) / cells.Count;
                // 파티클의 접점은 예고를 그렸던 체스판 윗면에 맞춘다.
                ground.y = surfaceHeight + .025f;
                _impactFeedback?.PlayPrimary(_selected, ground, _facing, CellSize, hit, contact);
            }
            else if (hit) _impactFeedback?.PlayFollowUpHit(contact);
            float pause = sweep ? _sweepHitPause : _selected == Move.Leap ? .09f : .07f;
            if (!primary || !hit || pause <= 0f || _elapsed >= ClipDuration || _animator == null ||
                _animator.runtimeAnimatorController == null || _animator.speed <= 0f || !_attacking) return;

            // 명중한 프레임에서 거신병 Animator만 멈춘다. 플레이어 넉백과 게임 시계는 계속 진행한다.
            _animatorSpeedBeforePause = _animator.speed;
            _hitPauseRemaining = pause;
            _elapsed = _impactTime;
            _animator.Play(States[(int)_selected], 0, _impactTime / ClipDuration);
            _animator.speed = 0f;
        }

        private void EnsureSweepTrace()
        {
            if (_traceStarted || _sweepPresentation == null) return;
            _traceStarted = true;
            _sweepPresentation.BeginGrid(_primary, CellSize, _facing, _board.SurfaceHeight);
        }

        private void RestoreAnimatorSpeed()
        {
            if (_animator != null) _animator.speed = _animatorSpeedBeforePause;
            _hitPauseRemaining = 0f;
        }

        private bool AdvanceLeap()
        {
            if (!_airborne)
            {
                if (!OnNavMesh) return false;
                _airborne = true;
                _updatePosition = Agent.updatePosition;
                _updateRotation = Agent.updateRotation;
                Agent.updatePosition = Agent.updateRotation = false;
                // 프레임 지연으로 정점을 지난 경우에는 출발 먼지를 뒤늦게 재생하지 않는다.
                if (_elapsed < LeapApexTime)
                    _impactFeedback?.PlayLeapTakeoff(_board.SurfaceHeight);
            }
            float phase = Mathf.InverseLerp(LeapTakeoffTime, LeapImpactTime, _elapsed);
            if (phase >= 1f) return EndLeap(_origin);
            // 상승 초반에 속도를 몰아 팔이 바닥을 밀어내는 동작과 루트 이동을 맞춘다.
            float height = _elapsed <= LeapApexTime
                ? 1f - Mathf.Pow(1f - Mathf.InverseLerp(LeapTakeoffTime, LeapApexTime, _elapsed), 3f)
                : 1f - Mathf.Pow(Mathf.InverseLerp(LeapApexTime, LeapImpactTime, _elapsed), 2f);
            // 도약 때 전진 대부분을 끝내고, 착지 직전에는 수평 이동보다 낙하가 두드러지게 한다.
            float travel = phase * (2f - phase);
            transform.position = Vector3.Lerp(_start, _origin, travel) + Vector3.up * (height * _jumpHeight);
            return true;
        }

        private bool EndLeap(Vector3 destination)
        {
            bool landed = Agent.isActiveAndEnabled && Agent.Warp(destination);
            if (!landed && Agent.isActiveAndEnabled) Agent.Warp(_start);
            transform.position = landed ? destination : _start;
            Agent.updatePosition = _updatePosition;
            Agent.updateRotation = _updateRotation;
            _airborne = false;
            return landed;
        }

        public override void Cancel()
        {
            if (_airborne) EndLeap(_start);
            if (_hitPauseRemaining > 0f) RestoreAnimatorSpeed();
            _sweepPresentation?.End();
            _gridEffects?.CancelRookCharge();
            _gridEffects?.HideWarnings();
            _attacking = _prepared = false;
            RestoreFacing();
            Warning?.Hide();
        }

        private static Vector3 Flat(Vector3 vector) { vector.y = 0f; return vector; }

        private void OnDisable() => Cancel();
    }
}
