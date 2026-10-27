using System;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using Unity.Behavior;
using UnityEngine;

namespace NKT.Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "ClipCompare", story: "[Enemy] clip [Operator] [Target]", category: "Conditions", id: "ccea402c1a06c3248a6ca27c94dab818")]
    public partial class ClipCompareCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [Comparison(comparisonType: ComparisonType.Boolean)]
        [SerializeReference] public BlackboardVariable<ConditionOperator> Operator;
        [SerializeReference] public BlackboardVariable<AnimParamSO> Target;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || Enemy.Value.Renderer == null)
                return false;
            
            Animator animator = Enemy.Value.Renderer.Animator;
            AnimatorStateInfo stateInfo;
            if (animator.IsInTransition(0))
            {
                stateInfo = animator.GetNextAnimatorStateInfo(0);
            }
            else
            {
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }
            
            return Operator.Value switch {
                ConditionOperator.Equal => stateInfo.shortNameHash == Target.Value.ParamHash,
                ConditionOperator.NotEqual => stateInfo.shortNameHash != Target.Value.ParamHash,
                _ => false
            };
        }
    }
}
