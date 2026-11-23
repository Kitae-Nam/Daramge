using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Hold
{
    public interface IHolder
    {
        event Action<IHoldable> OnReleased;
        IHoldable Current { get; }
        bool IsHolding { get; }
        bool IsBusy { get; }
        Vector3 HoldPosition { get; }
        bool Hold(IHoldable holdable);
        bool CanPlace(Vector3 rootPosition);
        void Release(Vector3 rootPosition, float duration);
    }
}