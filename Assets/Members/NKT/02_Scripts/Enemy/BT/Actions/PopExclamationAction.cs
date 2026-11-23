using System;
using NKT.Enemy.Module;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PopExclamation", story: "[Enemy] pop exclamation", category: "Action", id: "6d73f8b625a6ba0bc3f4a44c5df7c02f")]
    public partial class PopExclamationAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        protected override Status OnStart()
        {
            if(Enemy.Value == null )
                return Status.Failure;
            
            Enemy.Value.GetModule<EnemyNoticeModule>()?.Noticed();
            
            return Status.Success;
        }
    }
}

