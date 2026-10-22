using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

#if UNITY_EDITOR
namespace NKT.Enemy.BT.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/StateChannel")]
#endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "StateChannel", message: "Change [State]", category: "Events", id: "944b3a520eb8e66e15c9ab98347b52c5")]
    public sealed partial class StateChannel : EventChannel<EnemyState> { }
}

