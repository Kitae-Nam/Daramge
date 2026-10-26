using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>실행 가능한 패턴 선택만 담당한다. 쿨타임과 상태 전환은 러너가 최종 검증한다.</summary>
    [RequireComponent(typeof(BossPatternRunner), typeof(BossPhaseController), typeof(BossTarget))]
    public sealed class PhaseOneBrain : MonoBehaviour
    {
        [SerializeField] private BossPattern[] _patterns = new BossPattern[0];
        [SerializeField, Min(0f)] private float _openingDelay = 3f;
        [SerializeField, Min(0.1f)] private float _decisionInterval = 0.35f;
        private readonly List<BossPattern> _eligible = new List<BossPattern>();
        private BossPatternRunner _runner;
        private BossPhaseController _phase;
        private BossTarget _target;
        private BossSummonRoster _roster;
        private BossPattern _previous;
        private float _nextDecision;
        private void Awake()
        {
            _runner = GetComponent<BossPatternRunner>(); _phase = GetComponent<BossPhaseController>();
            _target = GetComponent<BossTarget>(); _roster = GetComponent<BossSummonRoster>();
        }
        private void OnEnable() => _nextDecision = Time.time + _openingDelay;
        private void Update()
        {
            if (Time.time < _nextDecision || _phase.CurrentPhase != 1 || !_target.HasTarget ||
                _runner.State != BossActionState.Idle || (_roster != null && _roster.IsAttackInProgress)) return;
            _nextDecision = Time.time + _decisionInterval;
            _eligible.Clear();
            foreach (BossPattern pattern in _patterns)
                if (_runner.IsReadyFor(pattern)) _eligible.Add(pattern);
            if (_eligible.Count == 0) return;
            // 다른 선택지가 있으면 직전 패턴을 빼서 연속 돌진/연속 포효를 줄인다.
            if (_eligible.Count > 1) _eligible.Remove(_previous);
            BossPattern selected = _eligible[Random.Range(0, _eligible.Count)];
            if (_runner.TryRun(selected)) _previous = selected;
        }
    }
}
