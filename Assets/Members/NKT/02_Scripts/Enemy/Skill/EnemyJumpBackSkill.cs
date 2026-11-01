using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using NKT.Enemy.Module;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class EnemyJumpBackSkill : AbstractEnemySkill
    {
        [SerializeField] private AnimParamSO rollAnimParam;
        [SerializeField] private float crossFadeDuration = 0.15f;

        private AgentTrigger _trigger;
        private EnemyNavRenderer _navRenderer;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _trigger = _ownerEnemy.GetModule<AgentTrigger>();
            _navRenderer = _ownerEnemy.GetModule<EnemyNavRenderer>();
        }

        public override bool CanUseSkill(GameObject target = null)
            => target != null && NormalizedCooldown >= 1f;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _ownerEnemy.AddStatus(AgentStatus.Invincible);

            if (_navRenderer != null)
                _navRenderer.UpdateRotationByAnimator = true;
            
            Vector3 direction = (_ownerEnemy.transform.position - target.transform.position).normalized;
            _ownerEnemy.transform.forward = -direction;
            
            _renderer.PlayClip(rollAnimParam.ParamHash, 0, crossFadeDuration);
            _trigger.OnAnimationEnd += StopSkill;
        }

        public override void StopSkill()
        {
            base.StopSkill();
            _trigger.OnAnimationEnd -= StopSkill;
            
            _ownerEnemy.RemoveStatus(AgentStatus.Invincible);
            if (_navRenderer !=  null)
                _navRenderer.UpdateRotationByAnimator = false;
        }
    }
}