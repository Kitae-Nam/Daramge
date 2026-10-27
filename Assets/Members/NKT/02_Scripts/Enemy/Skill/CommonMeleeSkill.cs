using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class CommonMeleeSkill : AbstractEnemySkill
    {
        [SerializeField] private AnimParamSO attackAnimParam;
        [SerializeField] private float crossFadeDuration = 0.15f;
        [SerializeField] private AbstractDamageCaster damageCaster;

        private AgentTrigger _trigger;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _ownerEnemy.GetModule<AgentTrigger>();
            Debug.Assert(_trigger != null, $"애니메이션 트리거가 있어야 근접 공격 스킬이 정상적으로 작동합니다. {gameObject}");
            Debug.Assert(damageCaster != null, $"데미지 캐스터가 없습니다. {gameObject}");
            damageCaster.InitCaster(_ownerEnemy);
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            if (target == null) return false;

            return NormalizedCooldown >= 1f && IsTargetInRange(target, SkillData.skillRange);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

            if (damageCaster is OverlapDamageCaster overlapCaster)
                overlapCaster.ResetHitCaster(); //이전 공격에서 맞은 대상 기록 초기화

            _trigger.OnDamageCast += HandleDamageCast;
            _trigger.OnAnimationEnd += HandleAnimationEnd;
            _renderer.PlayClip(attackAnimParam.ParamHash, 0, crossFadeDuration);
        }

        public override void StopSkill()
        {
            //피격 등으로 중간에 끊겨도 구독이 남지 않도록 둘 다 해제
            _trigger.OnDamageCast -= HandleDamageCast;
            _trigger.OnAnimationEnd -= HandleAnimationEnd;
            base.StopSkill();
        }

        private void HandleDamageCast()
        {
            damageCaster.CastDamage(damageCaster.transform.position, _ownerEnemy.transform.forward, SkillData);
        }

        private void HandleAnimationEnd()
        {
            if (_renderer.Animator.IsInTransition(0)) return; //이전 클립의 종료 이벤트는 무시
            StopSkill();
        }
    }
}
