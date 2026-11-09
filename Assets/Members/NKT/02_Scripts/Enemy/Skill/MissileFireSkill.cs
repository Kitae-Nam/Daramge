using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using NKT.Enemy.DamageCasters;
using NKT.Enemy.Module;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    //유도탄 한 발 발사. 발사 시점은 애니메이션의 DamageCastTrigger, 데미지는 총알이 맞았을 때 들어간다.
    public class MissileFireSkill : AbstractEnemySkill
    {
        [SerializeField] private AnimParamSO attackAnimParam;
        [SerializeField] private float crossFadeDuration = 0.15f;
        [SerializeField] private ProjectileFireDamageCaster projectileCaster;
        [Tooltip("총알이 나가는 위치. 비워두면 캐스터 위치")]
        [SerializeField] private Transform muzzle;

        private AgentTrigger _trigger;
        private AgentSensor _sensor;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _ownerEnemy.GetModule<AgentTrigger>();
            _sensor = _ownerEnemy.GetModule<AgentSensor>();

            Debug.Assert(projectileCaster != null, $"ProjectileFireDamageCaster가 없습니다. {gameObject}");
            projectileCaster.InitCaster(_ownerEnemy);
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
            projectileCaster.Target = target != null ? target.transform : null;

            _trigger.OnDamageCast += HandleFire;
            _trigger.OnAnimationEnd += StopSkill;
            _renderer.PlayClip(attackAnimParam.ParamHash, 0, crossFadeDuration);
        }

        private void HandleFire()
        {
            Transform firePoint = muzzle != null ? muzzle : projectileCaster.transform;
            projectileCaster.CastDamage(firePoint.position, _ownerEnemy.transform.forward, SkillData);
        }

        public override void StopSkill()
        {
            //피격 등으로 발사 전에 끊기면 총알이 나가지 않도록 둘 다 해제
            _trigger.OnDamageCast -= HandleFire;
            _trigger.OnAnimationEnd -= StopSkill;
            base.StopSkill();
        }
    }
}
