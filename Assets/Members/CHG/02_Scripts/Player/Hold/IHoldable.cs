using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Hold
{
    public interface IHoldable
    {
        event Action<IHoldable> OnHold;
        Transform Root { get; }
        bool CanHold { get; }
        float BottomOffset { get; }
        float GetReach(Vector3 direction);
        void BeginHold();
        void EndHold();
        bool CanPlace(Vector3 rootPosition, LayerMask obstacleMask);
    }
}