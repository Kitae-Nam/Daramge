using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public interface ILockOnModule
    {
        bool HasTarget { get; }
        Vector3 DirectionToTarget { get; }
    }
}