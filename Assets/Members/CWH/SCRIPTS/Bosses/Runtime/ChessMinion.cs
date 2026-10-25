using System;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    public enum ChessMinionKind { Pawn, Knight }

    /// <summary>
    /// 소환 기물 한 마리의 FSM이다. 폰은 접근 공격, 나이트는 착지점을 먼저 알리고 점프한다.
    /// 소환 가능 여부/보스 방어력은 Roster가 담당하므로 기물은 보스 체력을 직접 수정하지 않는다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(BreakableHealth))]
    public sealed class ChessMinion : MonoBehaviour
    {
        private enum State { Approaching, Warning, Jumping, Resting }
        [SerializeField] private ChessMinionKind _kind;
        [SerializeField] private Material _warningMaterial;
        [SerializeField, Min(0.1f)] private float _attackRadius = 2.5f;
        [SerializeField, Min(0f)] private float _damage = 12f;
        [SerializeField, Min(0.1f)] private float _warningTime = 1.3f;
        [SerializeField, Min(0.1f)] private float _restTime = 3f;
        private NavMeshAgent _agent;
        private BreakableHealth _health;
        private AreaTelegraphPresenter _warning;
        private Transform _target;
        private BossPatternRunner _ownerRunner;
        private BossSummonRoster _roster;
        private State _state;
        private float _timer;
        private Vector3 _landing;
        private Vector3 _jumpStart;
        private bool _initialized;
        private bool _removed;
        public ChessMinionKind Kind => _kind;
        public bool IsAttacking => _state == State.Warning || _state == State.Jumping;
        public event Action<ChessMinion> Removed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<BreakableHealth>();
            _warning = AreaTelegraphPresenter.Create(transform, _warningMaterial, "Minion warning");
        }
        private void OnEnable() => _health.Broken += OnBroken;
        public void Initialize(Transform target, float delay, BossPatternRunner ownerRunner = null)
        {
            _target = target;
            _ownerRunner = ownerRunner;
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
                    float triggerDistance = _kind == ChessMinionKind.Pawn ? _attackRadius : 8f;
                    if (offset.sqrMagnitude <= triggerDistance * triggerDistance &&
                        (_ownerRunner == null || (_ownerRunner.State == BossActionState.Idle && _ownerRunner.CurrentPattern == null)) &&
                        (_roster == null || !_roster.IsAttackInProgress)) StartWarning();
                    break;
                case State.Warning:
                    _warning.SetProgress(1f - Mathf.Max(0f, _timer) / _warningTime);
                    if (_timer > 0f) return;
                    if (_kind == ChessMinionKind.Pawn) { _warning.Hide(); Strike(); Rest(); }
                    else
                    {
                        _jumpStart = transform.position;
                        _agent.updatePosition = false;
                        _warning.SetProgress(1f);
                        _state = State.Jumping;
                        _timer = 0.8f;
                    }
                    break;
                case State.Jumping:
                    float progress = 1f - Mathf.Clamp01(_timer / 0.8f);
                    transform.position = Vector3.Lerp(_jumpStart, _landing, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 2.5f);
                    if (_timer > 0f) return;
                    _agent.Warp(_landing);
                    _agent.updatePosition = true;
                    Strike(); Rest();
                    break;
                case State.Resting:
                    if (_timer <= 0f) _state = State.Approaching;
                    break;
            }
        }
        private void StartWarning()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _landing = transform.position;
            if (_kind == ChessMinionKind.Knight)
            {
                Vector3 offset = _target.position - transform.position;
                offset.y = 0f;
                Vector3 desired = transform.position + Vector3.ClampMagnitude(offset, 7f);
                if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, _agent.areaMask)) return;
                _landing = hit.position;
            }
            _state = State.Warning;
            _timer = _warningTime;
            _warning.Show(_landing, Vector3.forward, _attackRadius);
        }
        private void Strike()
        {
            // 보스와 다른 소환 기물은 같은 편이므로 플레이어만 공격한다.
            BossAttackQuery.ApplySector(transform, _landing, Vector3.forward, _attackRadius, 360f, 5f,
                _damage, ~0, targetRoot: _target);
        }
        private void Rest() { _warning.Hide(); _state = State.Resting; _timer = _restTime; }
        private void OnBroken() { RemoveFromRoster(); Destroy(gameObject); }
        private void RemoveFromRoster()
        {
            if (_removed) return;
            _removed = true;
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
