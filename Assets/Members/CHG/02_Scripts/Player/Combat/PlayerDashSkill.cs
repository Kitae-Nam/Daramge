using System.Collections;
using Members.CHG._02_Scripts.Agent;
using Members.CHG._02_Scripts.CombatSystem;
using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using Members.CHG._02_Scripts.CoreSystem.DataSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player.Combat
{
    public class PlayerDashSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO dashParam;
        [SerializeField] private AnimationCurve dashCurve;
        [SerializeField] private float speed = 4f;
        [SerializeField] private float dashDuration = 0.4f;


        private AgentTrigger _trigger;
        
        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            Debug.Assert(_trigger != null, "애니메이션 트리거가 있어야 rolling 스킬이 정상작동합니다.");
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && !IsUsing;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            
            Vector3 worldMousePosition = _player.PlayerInput.GetWorldMousePosition();
            Vector3 direction = (worldMousePosition - _player.transform.position).normalized;
            _movement.RotateTo(direction);
            StartCoroutine(RollingCoroutine());
        }

        private IEnumerator RollingCoroutine()
        {
            _renderer.PlayClip(dashParam.ParamHash, 0f, 0.01f);
            _trigger.OnAnimationEnd += HandleAnimationEnd;
            _movement.CanManualMove = false;
            float currentDuration = 0f;
            Vector3 forward = _player.transform.forward;
            while (IsUsing)
            {
                float percent =  currentDuration / dashDuration;
                currentDuration += Time.fixedDeltaTime;
                float force = dashCurve.Evaluate(percent) * speed;
                _movement.SetMovementVelocity(forward * force);
                yield return new WaitForFixedUpdate();
            }
            
            _trigger.OnAnimationEnd -= HandleAnimationEnd;
            _movement.CanManualMove = true;
        }

        private void HandleAnimationEnd() => StopSkill();

    }
}