using System;
using NKT.Enemy.Module;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "FindTargetAction", story: "[Enemy] find [TargetGameObject]", category: "Action", id: "7206c963694185249a455a4cabb6d6a3")]
    public partial class FindTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        protected override Status OnStart()
        {
            if(Enemy.Value == null || Enemy.Value.AgentSensor == null)
                return Status.Failure;
            
            if(TargetGameObject.Value != null)
                return Status.Success;

            AgentSensor sensor = Enemy.Value.AgentSensor;

            int detectCount = sensor.FindTargetsInRadius(Enemy.Value.EnemyData.DetectRadius);
            if(detectCount <= 0) return Status.Failure;


            for (int i = 0; i < detectCount; i++)
            {
                Transform findTarget = sensor.ColliderResults[i].transform;
                if (!sensor.IsTargetInViewAngle(findTarget, Enemy.Value.EnemyData.ViewAngle))
                    continue; //시야각 안에 없다면 실패
                if(!sensor.IsTargetIsInSight(findTarget))
                    continue; //시야에 장애물이 있다면 실패
                
                TargetGameObject.Value = findTarget.gameObject;
                break;
            }
            
            return TargetGameObject.Value == null ? Status.Failure : Status.Success;
        }

        protected override Status OnUpdate()
        {
            return Status.Success;
        }

        protected override void OnEnd()
        {
        }
    }
}

