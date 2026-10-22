using System;
using Members.CHG._02_Scripts.CombatSystem;
using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player.Combat
{
    public class HeadbuttSkill : AbstractPlayerSkill, IChargeSkill
    {
        [SerializeField] private AnimParamSO chargeParam;
        [SerializeField] private AnimParamSO dashParam;
        [field: SerializeField] public ChargeLogic ChargeLogic { get; private set; }

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            ChargeLogic.Begin();
            _player.PlayerInput.OnScendSkillKeyReleased += ReleaseCharge;
        }

        private void Update()
        {
            ChargeLogic.Tick(Time.deltaTime);   
        }

        public void ReleaseCharge()
        {
            _player.PlayerInput.OnScendSkillKeyReleased -= ReleaseCharge;
            float power = ChargeLogic.Release();
            
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            return NormalizedCooldown >= 1f && !IsUsing;
        }

        public override void StopSkill()
        {
            ChargeLogic.Cancel();
            base.StopSkill();
        }
    }
}