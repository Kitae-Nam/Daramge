using System.Collections.Generic;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
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
        private readonly Dictionary<BossPattern, float> _lastUsed = new Dictionary<BossPattern, float>();
        private BossPatternRunner _runner;
        private BossPhaseController _phase;
        private BossTarget _target;
        private BossSummonRoster _roster;
        private BossPattern _previous;
        private float _nextDecision;
        private float _enabledAt;
        private ChessKingGridMovement _gridMovement;
        private BossCombatGate _combatGate;
        private void Awake()
        {
            _runner = GetComponent<BossPatternRunner>(); _phase = GetComponent<BossPhaseController>();
            _target = GetComponent<BossTarget>(); _roster = GetComponent<BossSummonRoster>();
            _gridMovement = GetComponent<ChessKingGridMovement>(); _combatGate = GetComponent<BossCombatGate>();
        }
        private void OnEnable()
        {
            _enabledAt = Time.time;
            _nextDecision = Time.time + _openingDelay;
            if (_combatGate != null) _combatGate.AvailabilityChanged += HandleCombatAvailability;
        }
        private void OnDisable() { if (_combatGate != null) _combatGate.AvailabilityChanged -= HandleCombatAvailability; }
        private void HandleCombatAvailability(bool open) { if (open) _nextDecision = Time.time + _openingDelay; }
        private void Update()
        {
            if (Time.time < _nextDecision) return;
            _nextDecision = Time.time + _decisionInterval;
            if (TrySelectPattern(out BossPattern selected) && _runner.TryRun(selected))
            { _previous = selected; _lastUsed[selected] = Time.time; }
        }

        /// <summary>거리 적합도와 대기 시간을 비교한다. 선택만으로 실행/쿨타임을 시작하지 않는다.</summary>
        public bool TrySelectPattern(out BossPattern selected)
        {
            selected = null;
            if (_phase.CurrentPhase != 1 || !_target.HasTarget || (_combatGate != null && !_combatGate.IsOpen) ||
                _runner.State != BossActionState.Idle || (_roster != null && _roster.IsAttackInProgress) ||
                (_gridMovement != null && _gridMovement.isActiveAndEnabled && (_gridMovement.IsStepping || !_gridMovement.IsCentered))) return false;
            Vector3 offset = _target.Target.position - transform.position; offset.y = 0f;
            float distance = offset.magnitude;
            float bestScore = float.NegativeInfinity;
            foreach (BossPattern pattern in _patterns)
            {
                if (!_runner.IsReadyFor(pattern)) continue;
                float suitability = pattern.GetSelectionScore(distance);
                if (suitability <= 0f) continue;
                float lastUsed = _lastUsed.TryGetValue(pattern, out float last) ? last : _enabledAt;
                // 오래 안 쓴 기술에 작은 가산점만 준다. 부적절한 거리(0점)는 대기해도 선택되지 않는다.
                float score = suitability + Mathf.Min(25f, (Time.time - lastUsed) * 2f);
                if (pattern == _previous) score -= 20f;
                if (score <= bestScore) continue;
                bestScore = score; selected = pattern;
            }
            return selected != null;
        }
    }
}
