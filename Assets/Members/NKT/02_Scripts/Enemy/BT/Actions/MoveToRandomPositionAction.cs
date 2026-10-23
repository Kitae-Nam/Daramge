using System;
using NKT.Enemy.Module;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "MoveToRandomPosition", story: "[Enemy] wander around home", category: "Action/Navigation", id: "13f0e761865b993ae780bd6823f7f6ff")]
    public partial class MoveToRandomPositionAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<float> MaxMoveTime = new(6f);

        private INavMovement _nav;
        private float _startTime;
        
        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.NavMovement == null)
                return Status.Failure;

            EnemyDataSO enemyData = Enemy.Value.EnemyData;
            _nav = Enemy.Value.NavMovement;
            
            if(!_nav.TryGetRandomPoint(
                   Enemy.Value.HomePoint, enemyData.WanderRadius, 
                   enemyData.MinWanderDistance, out Vector3 point))
                return Status.Failure;
            
            _nav.SetDestination(point);
            _startTime = Time.time;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_nav.IsArrived) return Status.Success;
            if(Time.time - _startTime > MaxMoveTime.Value) return Status.Failure;
            return Status.Running;
        }
    }
}

