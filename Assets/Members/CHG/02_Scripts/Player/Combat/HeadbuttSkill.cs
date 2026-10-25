using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class HeadbuttSkill : AbstractPlayerSkill, IChargeSkill
{
    [SerializeField] private AnimParamSO chargeParam;
    [SerializeField] private AnimParamSO dashParam;
    [SerializeField] private OverlapDamageCaster damageCaster;
    [field: SerializeField] public ChargeLogic ChargeLogic { get; private set; }

    [Header("Headbutt")]
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float minSpeed = 6f, maxSpeed = 14f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private AnimationCurve damageByCharge = AnimationCurve.Linear(0, 0.5f, 1, 2f);
    [SerializeField] private bool stopOnHit = true;

    public override void InitializeSkill(ISkillModule skillModule)
    {
        base.InitializeSkill(skillModule);
        Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
        damageCaster.InitCaster(_player);
    }

    public override bool CanUseSkill(GameObject target = null)
        => NormalizedCooldown >= 1f && !IsUsing;

    public override void UseSkill(GameObject target = null)
    {
        base.UseSkill(target);
        _movement.CanManualMove = false;
        _movement.SetMovementVelocity(Vector3.zero);
        _renderer.PlayClip(chargeParam.ParamHash, 0f, 0.1f);
        ChargeLogic.Begin();
    }

    private void Update() => ChargeLogic.Tick(Time.deltaTime);

    public void ReleaseCharge()
    {
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
            _movement.SetMovementVelocity(forward * dashCurve.Evaluate(percent) * speed);

            bool hit = damageCaster.CastDamage(damageCaster.transform.position, SkillData, multiplier);
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
        _movement.SetMovementVelocity(Vector3.zero);
        _movement.CanManualMove = true;
        base.StopSkill();
    }
}
}