using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    public enum ChessMinionKind { Pawn, Knight, Rook, Bishop }

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
        [SerializeField, Min(0.1f)] private float _attackRadius = 2.5f;
        [SerializeField, Min(0f)] private float _damage = 1f;
        [SerializeField, Min(0.1f)] private float _warningTime = 1.3f;
        [SerializeField, Min(0.1f)] private float _restTime = 3f;
        // 기존 프리팹의 값은 어댑터를 통해 보존한다. 새 기물의 행동은 _attack에 직접 연결한다.
        [SerializeField, HideInInspector] private float _lineAttackRange = 10f;
        [SerializeField, HideInInspector] private float _lineAttackAngle = 18f;
        private NavMeshAgent _agent;
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
        public bool IsAttacking => _state == State.Warning || _state == State.Attacking;
        public event Action<ChessMinion> Removed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<BreakableHealth>();
            _warning = AreaTelegraphPresenter.Create(transform, _warningMaterial, "Minion warning");
            if (_attack == null) _attack = GetComponent<ChessMinionAttack>();
            if (_attack == null) _attack = LegacyMinionAttackAdapter.Resolve(gameObject, _kind, _lineAttackRange, _lineAttackAngle);
            _attack.Initialize(_agent, _attackRadius, _damage, _warning);
        }
        private void OnEnable() => _health.Broken += OnBroken;

        public void Initialize(Transform target, float delay, BossPatternRunner ownerRunner = null)
        {
            _target = target;
            _ownerRunner = ownerRunner;
            // 소환한 보스의 페이즈로 피해량을 결정하고, 독립 기물은 기존 설정을 사용한다.
            float damage = BossAttackDamage.Resolve(ownerRunner != null ? ownerRunner.transform : null, _damage);
            _attack.Initialize(_agent, _attackRadius, damage, _warning);
            _roster = ownerRunner != null ? ownerRunner.GetComponent<BossSummonRoster>() : null;
            _initialized = true;
            _timer = delay;
            _state = State.Resting;
            if (_agent.isOnNavMesh) _agent.isStopped = true;
        }

        private void Update()
        {
            if (!_initialized || _removed || _health.IsDead || _target == null) return;
            _timer -= Time.deltaTime;
            switch (_state)
            {
                case State.Approaching:
                    if (!_agent.isOnNavMesh) return;
                    _agent.isStopped = false;
                    _agent.SetDestination(_target.position);
                    Vector3 offset = _target.position - transform.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= _attack.TriggerDistance * _attack.TriggerDistance &&
                        (_ownerRunner == null || (_ownerRunner.State == BossActionState.Idle && _ownerRunner.CurrentPattern == null)) &&
                        (_roster == null || !_roster.IsAttackInProgress)) StartWarning();
                    break;
                case State.Warning:
                    _warning.SetProgress(1f - Mathf.Max(0f, _timer) / _warningTime);
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

        private void StartWarning()
        {
            if (!_attack.Prepare(_target)) return;
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
