using System;
using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.FeedbackSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    [Serializable]
    public struct MeleeComboStep
    {  
        public AnimParamSO comboClip;
        public AnimationCurve comboCurve;
        public float comboDuration;
        public FeedbackPlayer hitFeedback;
    }
    
    public class PlayerMeleeCombo : AbstractPlayerSkill
    {
        [SerializeField] private AnimTriggerSO animationEndTrigger;
        [SerializeField] private AnimTriggerSO damageCastTrigger;

        [SerializeField] private MeleeComboStep[] comboSteps;
        
        [SerializeField] private float comboWindow = 0.4f;

        private AgentTrigger _trigger;
        private AbstractDamageCaster _damageCaster;
        private Coroutine _curCoroutine;
        private ILockOnModule _lockOnModule;
        private int _comboCounter = 0;

        public override void InitializeSkill(ISkillModule skillModule)  
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            _lockOnModule = _player.GetModule<PlayerLockOnModule>();
            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_trigger != null, $"trigger is null : {gameObject.name}");
            Debug.Assert(_damageCaster != null, $"damageCaster is null : {gameObject.name}");
            Debug.Assert(_lockOnModule != null, $"lockOnModule is null : {gameObject.name}");
            _damageCaster.InitCaster(skillModule.Owner);
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && IsUsing == false;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            bool comboCounterOver = _comboCounter >= comboSteps.Length;
            bool comboWindowExhausted = Time.time >= _lastUseTime + comboWindow;

            if (comboCounterOver || comboWindowExhausted)
            {
                _comboCounter = 0;
            }
    
            _renderer.PlayClip(comboSteps[_comboCounter].comboClip.ParamHash, 0f, 0.05f);

            _movement.RotateTo(GetAttackDirection()); // 추가
            _trigger.Subscribe(animationEndTrigger, HandleAnimationEnd);
            _trigger.Subscribe(damageCastTrigger, HandleDamageCast);
            _curCoroutine = StartCoroutine(SwordComboCoroutine());
        }

        private Vector3 GetAttackDirection() => _lockOnModule.HasTarget
            ? _lockOnModule.DirectionToTarget
            : _player.PlayerInput.GetWorldMousePosition() - _player.transform.position;

        private IEnumerator SwordComboCoroutine()
        {
            AnimationCurve comboCurve = comboSteps[_comboCounter].comboCurve;
            float comboDuration = comboSteps[_comboCounter].comboDuration / _skillModule.AttackSpeed;
            float currentDuration = 0f;
            Vector3 forward = _player.transform.forward;
            _movement.CanManualMove = false;
            WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
            while (IsUsing)
            {
                float percent = currentDuration / comboDuration;
                currentDuration += Time.fixedDeltaTime;
                float force = comboCurve.Evaluate(percent);
                _movement.SetMovementVelocity(forward * force);
                yield return waitForFixedUpdate;
            }
            
        }
        
        private void HandleDamageCast()
        {
            Vector3 position = _damageCaster.transform.position;
            bool isHit = _damageCaster.CastDamage(position, transform.forward, SkillData);

            if (isHit)
            {
                PlayFeedback(comboSteps[_comboCounter].hitFeedback, _damageCaster.LastHitPosition, _player.transform.forward);
            }
        }

        private void HandleAnimationEnd() => StopSkill();

        public override void StopSkill()
        {
            if (_curCoroutine != null)
            {
                StopCoroutine(_curCoroutine);
                _curCoroutine = null;
            }
            _movement.CanManualMove = true;
            _comboCounter++;
            
            _trigger.UnSubscribe(damageCastTrigger, HandleDamageCast);
            _trigger.UnSubscribe(animationEndTrigger, HandleAnimationEnd);
            base.StopSkill();
        }

        private void OnDestroy()
        {
            if (_trigger == null) return;
            _trigger.UnSubscribe(damageCastTrigger, HandleDamageCast);
            _trigger.UnSubscribe(animationEndTrigger, HandleAnimationEnd);
        }
    }
}