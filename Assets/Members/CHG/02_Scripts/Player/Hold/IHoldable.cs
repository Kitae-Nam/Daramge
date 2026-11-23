using UnityEngine;

namespace CHG._02_Scripts.Player.Hold
{
    public interface IHoldable
    {
        Transform Root { get; }
        bool CanHold { get; }
        float BottomOffset { get; }
        void BeginHold();
        void EndHold();
        bool CanPlace(Vector3 rootPosition, LayerMask obstacleMask);
    }
}