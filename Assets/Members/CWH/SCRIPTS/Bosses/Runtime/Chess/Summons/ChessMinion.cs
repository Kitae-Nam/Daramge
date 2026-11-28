using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons
{
    public enum ChessMinionKind { Pawn, Knight, Rook, Bishop, Colossus }

    /// <summary>
    /// 접근→경고→공격→휴식의 공통 순서와 생존 수명만 관리한다.
    /// 기물별 준비/판정/이동은 ChessMinionAttack 계약에 위임한다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(BreakableHealth))]
    public sealed class ChessMinion : MonoBehaviour
    {
        private enum State { Approaching, Warning, Attacking, Resting }
        [SerializeField] private ChessMinionKind _kind;
        [SerializeField] private Material _warningMaterial;
        [SerializeField] private ChessMinionAttack _attack;
        [SerializeField, Range(1, 2)] private int _summonCount = 2;
        [SerializeField, Min(0.1f)] private float _attackRadius = 2.5f;
        [SerializeField, Min(0f)] private float _damage = 1f;
        [SerializeField, Min(0.1f)] private float _warningTime = 1.3f;
        [SerializeField, Min(0.1f)] private float _restTime = 3f;
        // 기존 프리팹의 값은 어댑터를 통해 보존한다. 새 기물의 행동은 _attack에 직접 연결한다.
        [SerializeField, HideInInspector] private float _lineAttackRange = 10f;
        [SerializeField, HideInInspector] private float _lineAttackAngle = 18f;
        private NavMeshAgent _agent;
        private ChessMinionMovement _movement;
        private BreakableHealth _health;
        private AreaTelegraphPresenter _warning;
        private Transform _target;
        private BossPatternRunner _ownerRunner;
        private BossSummonRoster _roster;
        private State _state;
        private float _timer;
        private bool _initialized;
        private bool _removed;
        public ChessMinionKind Kind => _kind;
        public int SummonCount => Mathf.Clamp(_summonCount, 1, 2);
        public bool IsAttacking => _state == State.Warning || _state == State.Attacking;
        public event Action<ChessMinion> Removed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _movement = GetComponent<ChessMinionMovement>();
            _health = GetComponent<BreakableHealth>();
            _warning = AreaTelegraphPresenter.Create(transform, _warningMaterial, "Minion warning");
            if (_attack == null) _attack = GetComponent<ChessMinionAttack>();
            if (_attack == null) _attack = LegacyMinionAttackAdapter.Resolve(gameObject, _kind, _lineAttackRange, _lineAttackAngle);
            _attack.Initialize(_agent, _attackRadius, _damage, _warning);
        }
        private void OnEnable() => _health.Broken += OnBroken;
        private void Start()
        {
            // 씬에 직접 배치한 기물도 이동기에 지정한 대상을 사용한다. 보스가 초기화한 소환체는 그대로 둔다.
            if (!_initialized && _movement != null && _movement.StandaloneTarget != null)
                Initialize(_movement.StandaloneTarget, 0f);
        }

        public void Initialize(Transform target, float delay, BossPatternRunner ownerRunner = null)
        {
            _target = target;
            _ownerRunner = ownerRunner;
            // 소환한 보스의 페이즈로 피해량을 결정하고, 독립 기물은 기존 설정을 사용한다.
            float damage = BossAttackDamage.Resolve(ownerRunner != null ? ownerRunner.transform : null, _damage);
            _attack.Initialize(_agent, _attackRadius, damage, _warning);
            _roster = ownerRunner != null ? ownerRunner.GetComponent<BossSummonRoster>() : null;
            ChessBoardGrid board = ownerRunner != null ? ownerRunner.GetComponent<ChessBoardGrid>() : _movement?.Board;
            Transform summoner = ownerRunner != null ? ownerRunner.transform : null;
            _movement?.Bind(board, target, summoner);
            _attack.BindBoard(board, summoner);
            _initialized = true;
            _timer = delay;
            _state = State.Resting;
            if (_agent.isOnNavMesh) _agent.isStopped = true;
        }

        private void Update()
        {
            // 초기화 전에는 소환수 FSM이 아직 이동기를 소유하지 않는다.
            if (!_initialized) return;
            if (_removed || _health.IsDead || _target == null)
            {
                _movement?.Stop();
                if (!_removed && IsAttacking) { _attack.Cancel(); Rest(); }
                return;
            }
            _timer -= Time.deltaTime;
            switch (_state)
            {
                case State.Approaching:
                    if (AdvanceTowardsTarget() &&
                        (_ownerRunner == null || (_ownerRunner.State == BossActionState.Idle && _ownerRunner.CurrentPattern == null)) &&
                        (_roster == null || !_roster.IsAttackInProgress)) StartWarning();
                    break;
                case State.Warning:
                    float progress = 1f - Mathf.Max(0f, _timer) / _warningTime;
                    _warning.SetProgress(progress);
                    _attack.SetWarningProgress(progress);
                    if (_timer > 0f) return;
                    _attack.BeginAttack();
                    _state = State.Attacking;
                    if (_attack.Tick(0f)) Rest();
                    break;
                case State.Attacking:
                    if (_attack.Tick(Time.deltaTime)) Rest();
                    break;
                case State.Resting:
                    if (_timer <= 0f) _state = State.Approaching;
                    break;
            }
        }

        private bool AdvanceTowardsTarget()
        {
            // 이동 컴포넌트를 가진 기물만 새 규칙을 사용한다. 기존 소환수의 추적은 그대로 둔다.
            if (_movement != null)
                return _movement.isActiveAndEnabled && _movement.Advance(_attack.TriggerDistance, Time.deltaTime);
            if (!_agent.isOnNavMesh) return false;
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);
            Vector3 offset = _target.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _attack.TriggerDistance * _attack.TriggerDistance;
        }

        private void StartWarning()
        {
            if (!_attack.Prepare(_target)) return;
            _movement?.Stop();
            _agent.isStopped = true;
            _agent.ResetPath();
            _state = State.Warning;
            _timer = _warningTime;
            _attack.ShowWarning();
        }

        private void Rest() { _warning.Hide(); _state = State.Resting; _timer = _restTime; }
        private void OnBroken() { RemoveFromRoster(); Destroy(gameObject); }
        private void RemoveFromRoster()
        {
            if (_removed) return;
            _removed = true;
            _movement?.Stop();
            _attack.Cancel();
            _warning.Hide();
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
            Removed?.Invoke(this);
        }
        public void Despawn() { RemoveFromRoster(); Destroy(gameObject); }
        private void OnDisable()
        {
            _health.Broken -= OnBroken;
            RemoveFromRoster();
        }
    }
}
