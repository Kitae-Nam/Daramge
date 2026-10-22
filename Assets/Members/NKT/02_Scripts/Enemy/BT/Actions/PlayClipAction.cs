using System;
using Members.CHG._02_Scripts.CoreSystem.AnimationSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlayClip", story: "[Enemy] play [Clip] at [Layer] and [Position]", category: "Action/Animation", id: "35c6e7cda9171cf50e4db53c4c52c24a")]
    public partial class PlayClipAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<AnimParamSO> Clip;
        [SerializeReference] public BlackboardVariable<int> Layer;
        [SerializeReference] public BlackboardVariable<int> Position;
        
        [SerializeReference] public BlackboardVariable<float> CrossDuration = new(0.2f);

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.Renderer == null || Clip.Value == null)
                return Status.Failure;  
            
            Enemy.Value.Renderer.PlayClip(Clip.Value.ParamHash, Position.Value, CrossDuration.Value, Layer.Value);
            return Status.Success;
        }
    }
}

