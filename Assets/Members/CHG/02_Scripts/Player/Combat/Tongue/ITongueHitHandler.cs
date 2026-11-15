using UnityEngine;

namespace CHG._02_Scripts.Player.Combat.Tongue
{
    public interface ITongueHitHandler
    {
        void HandleTongueHit(Vector3 hitPoint);
    }
}