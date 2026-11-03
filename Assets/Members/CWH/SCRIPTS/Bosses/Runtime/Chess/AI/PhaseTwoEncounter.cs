using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 60% 경계의 정리/검은 킹 전환/전투 재개만 조율한다.
    /// 개별 공격 수치는 설정에, 패턴 선택은 기존 Brain에 맡겨 중복 AI를 만들지 않는다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossPhaseController), typeof(BossCombatGate))]
    public sealed class PhaseTwoEncounter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _transitionDuration = 2f;
        private BossPhaseController _phase;
        private BossCombatGate _gate;
        private BossMovementStateController _movement;
        private Coroutine _transition;
        public bool IsReady { get; private set; }

        private void Awake()
        {
            _phase = GetComponent<BossPhaseController>();
            _gate = GetComponent<BossCombatGate>();
            _movement = GetComponent<BossMovementStateController>();
        }

        private void OnEnable()
        {
            _phase.PhaseChanged += HandlePhase;
            HandlePhase(_phase.CurrentPhase);
        }

        private void HandlePhase(int phase)
        {
            if (phase == 1) return;
            IsReady = false;
            if (_transition != null) StopCoroutine(_transition);
            _transition = null;
            GetComponent<BossPatternRunner>()?.CancelCurrentPattern();
            GetComponent<BossSummonRoster>()?.Clear();
            GetComponent<BossSwordController>()?.Clear();
            _movement?.SetMovementAllowed(false);
            if (phase == 0)
            {
                _gate.Unblock(this);
                return;
            }
            _transition = StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            _gate.Block(this);
            // 컷신 차단과 다른 잠금이 남아 있으면 우리 전환이 끝나도 전투는 재개되지 않는다.
            yield return new WaitForSeconds(Mathf.Max(0f, _transitionDuration));
            if (_phase.CurrentPhase != 2) yield break;
            IsReady = true;
            _movement?.SetMovementAllowed(true);
            _gate.Unblock(this);
            _transition = null;
        }

        private void OnDisable()
        {
            if (_phase != null) _phase.PhaseChanged -= HandlePhase;
            if (_transition != null) StopCoroutine(_transition);
            _transition = null;
            IsReady = false;
            _movement?.SetMovementAllowed(false);
            if (_gate != null) _gate.Unblock(this);
        }
    }
}
