using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem
{
    public readonly struct FeedbackContext
    {
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }

        public FeedbackContext(Vector3 hitPoint, Vector3 hitDirection)
        {
            HitPoint = hitPoint;
            HitDirection = hitDirection;
        }
    }
}