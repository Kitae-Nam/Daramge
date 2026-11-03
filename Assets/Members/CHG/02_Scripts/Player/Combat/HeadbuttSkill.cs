using System;
using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CombatSystem.Indicator;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.FeedbackSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class HeadbuttSkill : AbstractPlayerSkill, IChargeSkill
    {
        private const int CURVE_SAMPLE_COUNT = 20;
        
        public bool IsHolding => ChargeLogic.IsCharging;
        [field: SerializeField] public ChargeLogic ChargeLogic { get; private set; }
        
        [SerializeField] private AnimParamSO chargeParam;
        [SerializeField] private AnimParamSO dashParam;
        [SerializeField] private OverlapDamageCaster damageCaster;

        [Header("Headbutt")]
        [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float minSpeed = 6f, maxSpeed = 14f;
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private AnimationCurve damageByCharge = AnimationCurve.Linear(0, 0.5f, 1, 2f);
        [SerializeField] private bool stopOnHit = true;
        [SerializeField] private FeedbackPlayer hitFeedback;

        private ISkillRangeIndicator _rangeIndicator;
        private float _dashCurveAverage;
        private float _castReach;
        
        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
            damageCaster.InitCaster(_player);
            
            _rangeIndicator = GetComponentInChildren<ISkillRangeIndicator>(true);
            Debug.Assert(_rangeIndicator != null, $"RangeIndicator is null : {gameObject.name}");
            _dashCurveAverage = CalculateCurveAverage(dashCurve);
            _castReach = Vector3.Dot(damageCaster.transform.position - _player.transform.position,
                _player.transform.forward) + damageCaster.Radius;
            ChargeLogic.OnChargeChanged += HandleChargeChanged;
        }

        private void OnDestroy() => ChargeLogic.OnChargeChanged -= HandleChargeChanged;

        public override bool CanUseSkill(GameObject target = null)
            => NormalizedCooldown >= 1f && !IsUsing;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _movement.CanManualMove = false;
            _movement.SetMovementVelocity(Vector3.zero);
            _renderer.PlayClip(chargeParam.ParamHash, 0f, 0.1f);
            ChargeLogic.Begin();
            _rangeIndicator.Show();
            UpdateRangeIndicator(0f);
        }

        private void Update() => ChargeLogic.Tick(Time.deltaTime);

        public void Release()
        {
            _rangeIndicator.Hide();
            if (!ChargeLogic.IsValidCharge)
            {
                StopSkill();
                return;
            }
            float power = ChargeLogic.Release();
            StartCoroutine(HeadbuttCoroutine(power));
        }

        private IEnumerator HeadbuttCoroutine(float power)
        {
            _renderer.PlayClip(dashParam.ParamHash, 0f, 0.01f);
            damageCaster.ResetHitCaster();

            Vector3 forward = _player.transform.forward;
            float speed = Mathf.Lerp(minSpeed, maxSpeed, power);
            float multiplier = damageByCharge.Evaluate(power);
            float elapsed = 0f;

            while (IsUsing && elapsed < dashDuration)
            {
                float percent = elapsed / dashDuration;
                _movement.SetMovementVelocity(forward * (dashCurve.Evaluate(percent) * speed));

                bool hit = damageCaster.CastDamage(damageCaster.transform.position, SkillData, multiplier);
                if (hit) PlayFeedback(hitFeedback, damageCaster.transform.position, forward, power);
                if (hit && stopOnHit) break;

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if (IsUsing) StopSkill();
        }

        public override void StopSkill()
        {
            StopAllCoroutines();
            ChargeLogic.Cancel();
            _rangeIndicator.Hide();
            _movement.SetMovementVelocity(Vector3.zero);
            _movement.CanManualMove = true;
            base.StopSkill();
        }
        
        private void HandleChargeChanged(float charge) => UpdateRangeIndicator(charge);

        private void UpdateRangeIndicator(float charge)
        {
            float max = GetReachLength(1f);
            _rangeIndicator.SetSize(new Vector2(damageCaster.Radius * 2f, max));
            _rangeIndicator.SetProgress(GetReachLength(charge) / max);
        }

        private float GetReachLength(float charge)
        {
            return GetDashDistance(charge) + _castReach;
        }

        private float GetDashDistance(float power)
            => Mathf.Lerp(minSpeed, maxSpeed, power) * _movement.MoveSpeed * dashDuration * _dashCurveAverage;

        private static float CalculateCurveAverage(AnimationCurve curve)
        {
            float sum = 0f;
            for (int i = 0; i < CURVE_SAMPLE_COUNT; i++)
                sum += curve.Evaluate((i + 0.5f) / CURVE_SAMPLE_COUNT);
            return sum / CURVE_SAMPLE_COUNT;
        }
    }
}