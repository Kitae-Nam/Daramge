using System;
using NKT.Enemy.Skill;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UseSkill", story: "[Enemy] use [SelectedSkill] to [TargetGameObject]", category: "Action/Combat", id: "c47b2e9f1a6d3085e2f9b4c71d6a8e30")]
    public partial class UseSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<EnemySkill> SelectedSkill;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private EnemySkill _skill;
        private bool _isFinished;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.SkillModule == null
                || SelectedSkill.Value == null || TargetGameObject.Value == null)
                return Status.Failure;

            _skill = SelectedSkill.Value;
            _isFinished = false;
            _skill.OnSkillFinished += HandleSkillFinished;
            Enemy.Value.SkillModule.UseSkill(_skill, TargetGameObject.Value.transform);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_isFinished) return Status.Success;
            return _skill.IsUsing ? Status.Running : Status.Failure; //외부에서 취소된 경우
        }

        protected override void OnEnd()
        {
            if (_skill == null) return;

            _skill.OnSkillFinished -= HandleSkillFinished;
            if (_skill.IsUsing) //피격 등으로 노드가 중단되면 스킬도 정리
                _skill.Cancel();
            _skill = null;
        }

        private void HandleSkillFinished() => _isFinished = true;
    }
}
