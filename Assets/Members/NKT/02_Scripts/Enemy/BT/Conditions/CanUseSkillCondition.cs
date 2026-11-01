using System;
using Unity.Behavior;
using UnityEngine;

namespace NKT.Enemy.BT.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "CanUseSkill", story: "[Enemy] can use [SkillNumber] to [TargetGameObject]", category: "Conditions", id: "3e8d1b7a5c924f06b1a7e3d9c58f2a64")]
    public partial class CanUseSkillCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<int> SkillNumber;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || Enemy.Value.SkillModule == null
                || SkillNumber.Value < 0 || TargetGameObject.Value == null)
                return false;

            return Enemy.Value.SkillModule.CanUseSkill(SkillNumber.Value, TargetGameObject.Value);
        }
    }
}
