using System;
using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class PlayerDashSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimTriggerSO animationEndTrigger;
        [SerializeField] private AnimParamSO dashParam;
        [SerializeField] private AnimationCurve dashCurve;
        [SerializeField] private float speed = 4f;
        [SerializeField] private float dashDuration = 0.4f;
        
        [Tooltip("test")] [SerializeField] private float staminaUseAmount = 1f;
        
        private AgentTrigger _trigger;
        private StaminaModule _stamina;
                
        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            _stamina = _player.GetModule<StaminaModule>();
            Debug.Assert(_trigger != null, $"Animation Trigger is null : {gameObject.name}");
            Debug.Assert(_stamina != null, $"StaminaModule is null : {gameObject.name}");
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && !IsUsing && _stamina.CanUseStamina(staminaUseAmount);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            if (_stamina.TryUseStamina(staminaUseAmount))
            {
                StartCoroutine(RollingCoroutine());
            }
        }

        private IEnumerator RollingCoroutine()
        {
            _renderer.PlayClip(dashParam.ParamHash, 0f, 0.01f);
            _trigger.Subscribe(animationEndTrigger, HandleAnimationEnd);
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
            
            _trigger.UnSubscribe(animationEndTrigger, HandleAnimationEnd);
            _movement.CanManualMove = true;
        }

        private void HandleAnimationEnd() => StopSkill();

        private void OnDestroy()
        {
            if (_trigger == null) return;
            _trigger.UnSubscribe(animationEndTrigger, HandleAnimationEnd);
        }
    }
}