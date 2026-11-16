using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public readonly struct LockOnTargetChangedEvent : IEvent
    {
        public PlayerController Player { get; }
        public Transform Target { get; }

        public LockOnTargetChangedEvent(PlayerController player, Transform target)
        {
            Player = player;
            Target = target;
        }
    }
}