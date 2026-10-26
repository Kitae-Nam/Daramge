using System;
using NKT.Enemy.Skill;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SelectSkill", story: "[Enemy] select skill for [TargetGameObject] into [SelectedSkill]", category: "Action/Combat", id: "8a3e6b1d4c7f2e9051b8d6a3f4c2e71d")]
    public partial class SelectSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<EnemySkill> SelectedSkill;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.SkillModule == null || TargetGameObject.Value == null)
                return Status.Failure;

            EnemySkill skill = Enemy.Value.SkillModule.SelectSkill(TargetGameObject.Value.transform);
            SelectedSkill.Value = skill;

            return skill != null ? Status.Success : Status.Failure; //쓸 수 있는 스킬이 없으면 실패
        }
    }
}
