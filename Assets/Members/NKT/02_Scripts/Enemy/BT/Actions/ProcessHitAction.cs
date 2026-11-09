using System;
using CHG._02_Scripts.CombatSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    //맞았을 때 때린 상대를 타겟으로 잡고 그쪽을 바라본다.
    //탐지 범위 밖에서 맞아도 바로 전투로 들어가게 하기 위함.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ProcessHit", story: "[Enemy] hit from [TargetGameObject]", category: "Action/Combat", id: "481fb76f999348c4ac0eea3ff316cdaa")]
    public partial class ProcessHitAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.ActionData == null)
                return Status.Failure;

            ActionDataModule actionData = Enemy.Value.ActionData;

            //함정 등 공격자가 없는 피격이면 타겟은 그대로 두고 맞은 방향만 본다.
            if (actionData.Attacker != null)
                TargetGameObject.Value = actionData.Attacker.gameObject;

            RotateToHitDirection(actionData);
            return Status.Success;
        }

        private void RotateToHitDirection(ActionDataModule actionData)
        {
            Transform enemyTrm = Enemy.Value.transform;

            //공격자가 있으면 공격자 쪽, 없으면 HitNormal(맞은 면에서 바깥 방향) 쪽을 본다.
            Vector3 direction = actionData.Attacker != null
                ? actionData.Attacker.transform.position - enemyTrm.position
                : actionData.HitNormal;
            direction.y = 0;

            if (direction.sqrMagnitude < 0.0001f) return;
            enemyTrm.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }
}
