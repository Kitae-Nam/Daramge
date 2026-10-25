using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>1페이즈 종료 시 여러 부품의 정리 순서를 조율한다. 2페이즈 공격은 별도 구현 지점이다.</summary>
    [RequireComponent(typeof(BossPhaseController), typeof(BossPatternRunner), typeof(BossMovementStateController))]
    public sealed class PhaseOneEncounter : MonoBehaviour
    {
        [SerializeField] private BossPattern[] _phaseOnePatterns = new BossPattern[0];
        private BossPhaseController _phase;
        private BossPatternRunner _runner;
        private BossMovementStateController _movement;
        private void Awake() { _phase = GetComponent<BossPhaseController>(); _runner = GetComponent<BossPatternRunner>(); _movement = GetComponent<BossMovementStateController>(); }
        private void OnEnable() { _phase.PhaseChanged += HandlePhase; HandlePhase(_phase.CurrentPhase); }
        private void OnDisable() { _phase.PhaseChanged -= HandlePhase; }
        private void HandlePhase(int phase)
        {
            if (phase == 1) return;
            _runner.CancelCurrentPattern();
            _movement.SetMovementAllowed(false);
            foreach (BossPattern pattern in _phaseOnePatterns) if (pattern != null) pattern.enabled = false;
            GetComponent<BossSummonRoster>()?.Clear();
            GetComponent<BossSwordController>()?.Clear();
            GetComponent<BossCombatPresentation>()?.ResetWeapon();
            // 60% 도달 뒤 1페이즈 AI가 계속 공격하지 않도록 경계에서 멈춘다.
            if (phase == 2) Debug.Log("1페이즈 완료: HP 60% 이하. 2페이즈 구현을 기다리는 상태입니다.", this);
        }
    }
}
