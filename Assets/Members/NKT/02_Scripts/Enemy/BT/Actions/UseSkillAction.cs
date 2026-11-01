using System;
using CHG._02_Scripts.CombatSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UseSkill", story: "[Enemy] use [SkillNumber] to [TargetGameObject]", category: "Action/Combat", id: "c47b2e9f1a6d3085e2f9b4c71d6a8e30")]
    public partial class UseSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<int> SkillNumber;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private ISkillModule _skillModule;
        private bool _isSkillEnd;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.SkillModule == null || SkillNumber.Value < 0)
            {
                Debug.LogWarning("Enemy, SkillModule 또는 SkillNumber가 올바르지 않습니다. 행동이 실패로 간주됩니다.");
                return Status.Failure;
            }

            _skillModule = Enemy.Value.SkillModule;
            _isSkillEnd = false;
            _skillModule.OnCurrentSkillEnd += HandleSkillEnd; //UseSkill 전에 구독해야 종료를 놓치지 않는다.
            _skillModule.UseSkill(SkillNumber.Value, TargetGameObject.Value);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return _isSkillEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if (_skillModule == null) return;

            _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
            _skillModule.StopSkillIfNotFinished(); //피격 등으로 노드가 중단되면 스킬도 정리
            _skillModule = null;
        }

        private void HandleSkillEnd() => _isSkillEnd = true;
    }
}
