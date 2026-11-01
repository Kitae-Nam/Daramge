using System;
using NKT.Enemy.Module;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ChaseToTarget", story: "[Enemy] chase to [TargetGameObject]", category: "Action/Navigation", id: "5d1f7c2a9e8b4f61a3c07d94e2b6f158")]
    public partial class ChaseToTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<float> RepathDistance = new(1f);

        private INavMovement _navMovement;
        private Vector3 _destination;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.NavMovement == null || TargetGameObject.Value == null)
                return Status.Failure;

            _navMovement = Enemy.Value.NavMovement;
            _destination = TargetGameObject.Value.transform.position;
            if (IsInStopDistance()) return Status.Failure;
            _navMovement.SetDestination(_destination);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (TargetGameObject.Value == null) //쫓던 대상이 사라지면
                return Status.Failure;

            if (IsInStopDistance())
            {
                _navMovement.NavAgent.ResetPath();
                return Status.Success;
            }
            Vector3 newDestination = TargetGameObject.Value.transform.position;
            if (Vector3.Distance(_destination, newDestination) > RepathDistance.Value)
            {
                _destination = newDestination;
                _navMovement.SetDestination(_destination);
            }

            return Status.Running;
        }
        
        private bool IsInStopDistance()
        {
            Vector3 diff = TargetGameObject.Value.transform.position - Enemy.Value.transform.position;
            diff.y = 0;
            return diff.magnitude <= Enemy.Value.EnemyData.StopDistance;
        }
    }
}
