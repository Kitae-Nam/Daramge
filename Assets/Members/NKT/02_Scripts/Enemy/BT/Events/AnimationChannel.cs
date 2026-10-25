using System;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

#if UNITY_EDITOR
namespace NKT.Enemy.BT.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/AnimationChannel")]
#endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "AnimationChannel", message: "Play [Clip]", category: "Events", id: "0412b5dbaadbfc98dd014ecd5cbd05da")]
    public sealed partial class AnimationChannel : EventChannel<AnimParamSO> { }
}

