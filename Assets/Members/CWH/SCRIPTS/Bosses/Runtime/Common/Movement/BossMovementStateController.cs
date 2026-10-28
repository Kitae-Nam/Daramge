using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement
{
    /// <summary>
    /// 보스 패턴 상태와 이동을 연결하는 스크립트입니다. 
    /// </summary>
    [RequireComponent(typeof(BossPatternRunner), typeof(BossMovement), typeof(BossFacing))]
    public class BossMovementStateController : MonoBehaviour
    {
        private BossPatternRunner _bossPatternRunner;
        [SerializeField] private MonoBehaviour _movementDriver;
        private IBossChaseMovement _bossMovement;
        private BossCombatGate _combatGate;
        private BossFacing _bossFacing;
        private bool _movementAllowed = true;

        private void Awake()
        {
            // Inspector에서 NavMesh 추적/킹 그리드 이동 중 하나만 연결한다.
            _bossMovement = _movementDriver as IBossChaseMovement ?? GetComponent<BossMovement>();
            _combatGate = GetComponent<BossCombatGate>();
            _bossPatternRunner = GetComponent<BossPatternRunner>();
            _bossFacing = GetComponent<BossFacing>();
        }

        private void OnEnable()
        {
            _bossPatternRunner.StateChanged += HandleStateChange;
            if (_combatGate != null) _combatGate.AvailabilityChanged += HandleCombatAvailability;
            
            HandleStateChange(_bossPatternRunner.State);  
        }

        private void OnDisable()
        {
            _bossPatternRunner.StateChanged -= HandleStateChange;
            if (_combatGate != null) _combatGate.AvailabilityChanged -= HandleCombatAvailability;
            
            _bossMovement.SetChaseEnabled(false);
            _bossFacing.SetFacingEnabled(false);
        }
        
        private void HandleStateChange(BossActionState state)
        {
            
            bool allowed = _movementAllowed && (_combatGate == null || _combatGate.IsOpen) && state == BossActionState.Idle;

            _bossMovement.SetChaseEnabled(allowed);
            _bossFacing.SetFacingEnabled(allowed);
            
        }

        private void HandleCombatAvailability(bool open) => HandleStateChange(_bossPatternRunner.State);

        /// <summary>페이즈 종료처럼 패턴 바깥의 정지 사유도 Idle 전환으로 풀리지 않게 보존한다.</summary>
        public void SetMovementAllowed(bool allowed)
        {
            _movementAllowed = allowed;
            HandleStateChange(_bossPatternRunner.State);
        }
    }
}
