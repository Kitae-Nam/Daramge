using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public interface IControlMovement
    {
        bool CanManualMove { get; set; }
        bool CanUseGravity { get; set; }
        void SetMovementVelocity(Vector3 velocity);
        void SetMovementDirection(Vector2 movementInput);
        void RotateTo(Vector3 direction);
    }
}