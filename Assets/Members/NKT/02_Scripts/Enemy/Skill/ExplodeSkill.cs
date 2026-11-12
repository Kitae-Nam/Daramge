using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using GGMLib.EventSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace NKT.Enemy.Skill
{
    public class ExplodeSkill : AbstractEnemySkill
    {
        [SerializeField] private PoolItemSO explosionEffect;
        [SerializeField] private GameEventChannelSO createChannel;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private AbstractDamageCaster damageCaster;

        private AgentTrigger _trigger;
        
        public override bool CanUseSkill(GameObject target = null)
        {
            if(target == null) 
                return false;
            
            return IsTargetInRange(target, SkillData.skillRange);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            if (damageCaster is OverlapDamageCaster overlapCaster)
                overlapCaster.ResetHitCaster(); //이전 공격에서 맞은 대상 기록 초기화

            _trigger.OnDamageCast += HandleDamageCast;
        }

        private void HandleDamageCast()
        {
            _trigger.OnDamageCast -= HandleDamageCast;
            base.StopSkill();
        }
    }
}