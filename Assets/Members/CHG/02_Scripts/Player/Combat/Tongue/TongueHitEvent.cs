using UnityEngine;
using UnityEngine.Events;

namespace CHG._02_Scripts.Player.Combat.Tongue
{
    public class TongueHitEvent : MonoBehaviour, ITongueHitHandler
    {
        [SerializeField] private UnityEvent<Vector3> onTongueHit;
        
        public void HandleTongueHit(Vector3 hitPoint) => onTongueHit?.Invoke(hitPoint);
    }
}