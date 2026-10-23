using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StopEnemy", story: "[Enemy] stop", category: "Action/Navigation", id: "ad816e9169e6401a5803a7e340dad876")]
    public partial class StopEnemyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        protected override Status OnStart()
        {
            if(Enemy.Value == null || Enemy.Value.NavMovement == null)
                return Status.Failure;
            
            Enemy.Value.NavMovement.NavAgent.ResetPath();
            return Status.Success;
        }
    }
}

