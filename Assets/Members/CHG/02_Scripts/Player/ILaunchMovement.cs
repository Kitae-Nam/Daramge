using System;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public interface ILaunchMovement
    {
        event Action OnLanded;
        bool IsLaunched { get; }
        float Gravity { get; }
        void Launch(Vector3 launchVelocity, float riseGravityScale = 1f, float fallGravityScale = 1f);
        void CancelLaunch();
    }
}