using System;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public interface ILaunchMovement
    {
        event Action OnLanded;
        bool IsLaunched { get; }
        float Gravity { get; }
        void Launch(Vector3 launchVelocity);
        void CancelLaunch();
    }
}