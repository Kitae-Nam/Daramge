using System;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CombatSystem.DotSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class HeatSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimTriggerSO animEndTrigger;
        [SerializeField] private AnimParamSO heatParam;
        [SerializeField] private BuffSO heatBuff;
        [SerializeField] private DotSO burnDot;
        [SerializeField] private float staminaUseAmount = 1f;

        private AgentTrigger _trigger;
        private StaminaModule _stamina;
        private IBuffModule _buffModule;
        private IHitDotModule _hitDotModule;
        private bool _isHeated;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            _stamina = _player.GetModule<StaminaModule>();
            _buffModule = _player.GetModule<IBuffModule>();
            _hitDotModule = _player.GetModule<IHitDotModule>();
            Debug.Assert(_trigger != null, $"trigger is null : {gameObject.name}");
            Debug.Assert(_stamina != null, $"staminaModule is null : {gameObject.name}");
            Debug.Assert(_buffModule != null, $"buffModule is null : {gameObject.name}");
            Debug.Assert(_hitDotModule != null, $"hitDotModule is null : {gameObject.name}");

            _buffModule.OnBuffEnded += HandleBuffEnded;
        }

        private void OnDestroy()
        {
            if (_trigger != null)
                _trigger.UnSubscribe(animEndTrigger, HandleAnimationEnd);
            if (_buffModule != null)
                _buffModule.OnBuffEnded -= HandleBuffEnded;
        }

        public override bool CanUseSkill(GameObject target = null)
            => !_isHeated && !IsUsing && NormalizedCooldown >= 1f && 
               _stamina.CanUseStamina(staminaUseAmount);

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _stamina.TryUseStamina(staminaUseAmount);
            _movement.CanManualMove = false;
            _movement.SetMovementVelocity(Vector3.zero);
            _renderer.PlayClip(heatParam.ParamHash, 0f,0.05f);
            _trigger.Subscribe(animEndTrigger, HandleAnimationEnd);
        }

        private void HandleAnimationEnd()
        {
            _isHeated = true;
            _buffModule.ApplyBuff(heatBuff);
            _hitDotModule.AddDot(burnDot);
            StopSkill();
        }

        private void HandleBuffEnded(BuffSO buff)
        {
            if (buff != heatBuff) return;
            
            _isHeated = false;
            _hitDotModule.RemoveDot(burnDot);
            _lastUseTime = Time.time;
        }

        public override void StopSkill()
        {
            _trigger.UnSubscribe(animEndTrigger, HandleAnimationEnd);
            _movement.CanManualMove = true;
            base.StopSkill();
        }
    }
}