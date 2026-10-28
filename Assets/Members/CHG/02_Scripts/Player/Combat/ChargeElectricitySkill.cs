using System;
using System.Collections;
using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class ChargeElectricitySkill : AbstractPlayerSkill, IChargeSkill
    {
        public bool IsHolding => ChargeLogic.IsCharging;
        [field:SerializeField] public ChargeLogic ChargeLogic { get; private set; }

        [SerializeField] private AnimParamSO chargeParam;
        [SerializeField] private AnimParamSO impactParam;
        [SerializeField] private OverlapDamageCaster damageCaster;
        [SerializeField] private StatSO moveSpeedStat;

        [SerializeField] private float minRadius = 1.5f;
        [SerializeField] private float maxRadius = 4f;
        [SerializeField] private AnimationCurve chargeDamageCurve;
        [SerializeField, Range(0f, 1f)] private float chargeSlowRatio = 0.4f;
        [SerializeField] private float recoveryTime = 0.4f;

        private IStatModule _statModule;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _statModule = _player.GetModule<IStatModule>();
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
            damageCaster.InitCaster(_player);
        }

        public override bool CanUseSkill(GameObject target = null)
            => NormalizedCooldown >= 1f && !IsUsing;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _renderer.PlayClip(chargeParam.ParamHash, 0f, 0.1f);
            ChargeLogic.Begin();
            _movement.CanManualMove = true;
            ApplyChargeSlow(); 
        }

        private void Update() => ChargeLogic.Tick(Time.deltaTime);


        public void Release()
        {
            float power = ChargeLogic.Release();
            if (!ChargeLogic.IsValidCharge)
                power = 0f;

            RemoveChargeSlow();
            _movement.CanManualMove = false;
            _movement.SetMovementVelocity(Vector3.zero);
            StartCoroutine(SlamCoroutine(power));
        }

        private IEnumerator SlamCoroutine(float power)
        {
            _renderer.PlayClip(impactParam.ParamHash, 0f, 0.1f);
            damageCaster.ResetHitCaster();
            
            float radius = Mathf.Lerp(minRadius, maxRadius, power);
            float multiplier = chargeDamageCurve.Evaluate(power);
            damageCaster.CastDamage(damageCaster.transform.position, SkillData, multiplier, radius);
            
            yield return new WaitForSeconds(recoveryTime);
            if (IsUsing) StopSkill();
        }

        public override void StopSkill()
        {
            StopAllCoroutines();
            ChargeLogic.Cancel();
            RemoveChargeSlow();
            _movement.CanManualMove = true;
            base.StopSkill();
        }

        private void ApplyChargeSlow()
        {
            if (_statModule.TryGetStat(moveSpeedStat.AssetIndex, out StatSO stat))
                _statModule.AddModifier(moveSpeedStat.AssetIndex, this, -stat.Value * chargeSlowRatio);
            
        }

        private void RemoveChargeSlow()
        {
            _statModule.RemoveModifier(moveSpeedStat.AssetIndex, this);
        }

        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (damageCaster == null) return;
            Vector3 center = damageCaster.transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, minRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, maxRadius);
            Gizmos.color = Color.white;
        }
#endif
    }
}