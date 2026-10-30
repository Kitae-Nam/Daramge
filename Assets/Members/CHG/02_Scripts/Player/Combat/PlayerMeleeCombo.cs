using System;
using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CoreSystem.Events;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    [Serializable]
    public struct MeleeComboStep
    {  
        public AnimParamSO comboClip;
        public AnimationCurve comboCurve;
        public float comboDuration;
    }
    
    public class PlayerMeleeCombo : AbstractPlayerSkill
    {
        [SerializeField] private AnimTriggerSO animationEndTrigger;
        [SerializeField] private AnimTriggerSO damageCastTrigger;
        [SerializeField] private PoolItemSO slashVfxItem;
        [SerializeField] private PoolItemSO impactVfxItem;

        [SerializeField] private MeleeComboStep[] comboSteps;
        
        [SerializeField] private float comboWindow = 0.4f;

        private AgentTrigger _trigger;
        private AbstractDamageCaster _damageCaster;

        private Coroutine _curCoroutine;
        private int _comboCounter = 0;

        public override void InitializeSkill(ISkillModule skillModule)  
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            Debug.Assert(_trigger != null, $"trigger is null : {gameObject.name}");
            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_damageCaster != null, $"damageCaster is null : {gameObject.name}");
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

            Vector3 mousePosition = _player.PlayerInput.GetWorldMousePosition();
            Vector3 direction = mousePosition - _player.transform.position;
            _movement.RotateTo(direction.normalized);
            _trigger.Subscribe(animationEndTrigger, HandleAnimationEnd);
            _trigger.Subscribe(damageCastTrigger, HandleDamageCast);
            _curCoroutine = StartCoroutine(SwordComboCoroutine());
        }

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
                if (slashVfxItem != null)
                    _skillModule.CreateChannel.RaiseEvent(
                        CreateEvents.ShowPoolingEffect.InitData(slashVfxItem, _damageCaster.LastHitPosition, Quaternion.identity));
                if (impactVfxItem != null)
                    _skillModule.CreateChannel.RaiseEvent(
                        CreateEvents.ShowPoolingEffect.InitData(impactVfxItem, _damageCaster.LastHitPosition, Quaternion.identity));
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