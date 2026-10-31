using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using NKT.Enemy.Module;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class CommonRangeSkill : AbstractEnemySkill
    {
        [SerializeField] private AnimParamSO attackAnimParam;
        [SerializeField] private float crossFadeDuration = 0.15f;

        private AgentTrigger _trigger;
        private AgentSensor _sensor;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _ownerEnemy.GetModule<AgentTrigger>();
            _sensor = _ownerEnemy.GetModule<AgentSensor>();
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            if (target == null) return false;

            Vector3 distance = target.transform.position - _ownerEnemy.transform.position;
            distance.y = 0;

            return NormalizedCooldown >= 1f && distance.magnitude <= SkillData.skillRange
                                            && _sensor.IsTargetIsInSight(target.transform);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _renderer.PlayClip(attackAnimParam.ParamHash, 0, crossFadeDuration);
            _trigger.OnAnimationEnd += StopSkill;
        }

        public override void StopSkill()
        {
            base.StopSkill();
            _trigger.OnAnimationEnd -= StopSkill;
        }
    }
}