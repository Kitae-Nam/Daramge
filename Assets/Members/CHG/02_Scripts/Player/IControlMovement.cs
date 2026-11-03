using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public interface IControlMovement
    {
        bool CanManualMove { get; set; }
        bool CanAutoRotate { get; set; }
        bool CanUseGravity { get; set; }
        float MoveSpeed { get; }
        void SetMovementVelocity(Vector3 velocity);
        void SetMovementDirection(Vector2 movementInput);
        void RotateTo(Vector3 direction);
        Vector3 ToWorldDirection(Vector2 movementInput);
        
    }
}