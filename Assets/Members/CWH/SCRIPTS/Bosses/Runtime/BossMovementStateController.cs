using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 보스 패턴 상태와 이동을 연결하는 스크립트입니다. 
    /// </summary>
    [RequireComponent(typeof(BossPatternRunner), typeof(BossMovement), typeof(BossFacing))]
    public class BossMovementStateController : MonoBehaviour
    {
        private BossPatternRunner _bossPatternRunner;
        private BossMovement _bossMovement;
        private BossFacing _bossFacing;

        private void Awake()
        {
            _bossMovement = GetComponent<BossMovement>();
            _bossPatternRunner = GetComponent<BossPatternRunner>();
            _bossFacing = GetComponent<BossFacing>();
        }

        private void OnEnable()
        {
            _bossPatternRunner.StateChanged += HandleStateChange;
            
            HandleStateChange(_bossPatternRunner.State);  
        }

        private void OnDisable()
        {
            _bossPatternRunner.StateChanged -= HandleStateChange;
            
            _bossMovement.SetChaseEnabled(false);
            _bossFacing.SetFacingEnabled(false);
        }
        
        private void HandleStateChange(BossActionState state)
        {
            
            bool allowed = state == BossActionState.Idle;

            _bossMovement.SetChaseEnabled(allowed);
            _bossFacing.SetFacingEnabled(allowed);
            
        }
    }
}