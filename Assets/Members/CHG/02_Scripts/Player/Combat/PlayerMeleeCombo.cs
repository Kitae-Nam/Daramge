using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.DataSystem;
using CHG._02_Scripts.CoreSystem.EffectSystem;
using CoreSystem.Events;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class PlayerMeleeCombo : AbstractPlayerSkill
    {
        [SerializeField] private PoolItemSO slashVfxItem;
        [SerializeField] private PoolItemSO impactVfxItem;
        
        [SerializeField] private AnimParamSO[] comboClips;
        [SerializeField] private AnimationCurve[] comboCurves;
        [SerializeField] private float[] comboDurations;
        [SerializeField] private AssetNameSO[] comboEffects;
        
        [SerializeField] private float comboWindow = 0.4f;

        private AgentTrigger _trigger;
        private VfxModule _vfxModule;
        private AbstractDamageCaster _damageCaster;
        
        public float _attackSpeed = 1f;
        public int ComboCounter { get; set; } = 0;

        public override void InitializeSkill(ISkillModule skillModule)  
        {
            base.InitializeSkill(skillModule);
            _trigger = _player.GetModule<AgentTrigger>();
            Debug.Assert(_trigger != null, $"trigger is null : {gameObject.name}");
            _vfxModule = _player.GetModule<VfxModule>();
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
            bool comboCounterOver = ComboCounter >= comboClips.Length;
            bool comboWindowExhausted = Time.time >= _lastUseTime + comboWindow;

            if (comboCounterOver || comboWindowExhausted)
            {
                ComboCounter = 0;
            }
            
            // if (comboEffects.Length < ComboCounter - 1)
                // _vfxModule.PlayVfx(comboEffects[ComboCounter].AssetHash);
            _renderer.PlayClip(comboClips[ComboCounter].ParamHash, 0f, 0.05f);

            Vector3 mousePosition = _player.PlayerInput.GetWorldMousePosition();
            Vector3 direction = mousePosition - _player.transform.position;
            _movement.RotateTo(direction.normalized);
            
            StartCoroutine(SwordComboCoroutine());
        }

        private IEnumerator SwordComboCoroutine()
        {
            _trigger.OnAnimationEnd += HandleAnimationEnd;
            _trigger.OnDamageCast += HandleDamageCast;
            
            AnimationCurve comboCurve = comboCurves[ComboCounter];
            float comboDuration = comboDurations[ComboCounter];
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
            
            
            _movement.CanManualMove = true;
            _trigger.OnDamageCast -= HandleDamageCast;
            _trigger.OnAnimationEnd -= HandleAnimationEnd;
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
            ComboCounter++;
            _trigger.OnAnimationEnd -= HandleAnimationEnd;
            base.StopSkill();
        }
    }
}