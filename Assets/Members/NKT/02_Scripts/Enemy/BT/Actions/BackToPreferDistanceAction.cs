using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Back to prefer distance", story: "[Enemy] retreat form [TargetGameObject]", category: "Action/Combat", id: "707dff05194e003142f80424c53fafbc")]
    public partial class BackToPreferDistanceAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private Transform _targetTrm;
        private Transform _selfTrm;
        private float _preferDistance;
        
        protected override Status OnStart()
        {
            if (TargetGameObject.Value == null || Enemy.Value is not IHasRangeData rangeEnemy)
                return Status.Failure;

            _selfTrm = Enemy.Value.transform;
            _targetTrm = TargetGameObject.Value.transform;
            _preferDistance = rangeEnemy.RangeData.PreferDistance;
            
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            Vector3 distance = _targetTrm.position - _selfTrm.position;
            _selfTrm.forward = distance.normalized;

            if (distance.magnitude > _preferDistance)
                return Status.Success;
            
            return Status.Running;
        }
    }
}

