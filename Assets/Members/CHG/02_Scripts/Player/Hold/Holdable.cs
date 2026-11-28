using System;
using CHG._02_Scripts.Player.Combat.Tongue;
using UnityEngine;

namespace CHG._02_Scripts.Player.Hold
{
    public class Holdable : TonguePullable, IHoldable
    {
        public event Action<IHoldable> OnHold;
        
        private const float PLACE_CHECK_SCALE = 0.9f;

        private bool _isHeld;

        public bool CanHold => !_isHeld;
        public float BottomOffset => GetReach(Vector3.down);   

        public void BeginHold()
        {
            _isHeld = true;
            BeginPull();
            OnHold?.Invoke(this);
        }

        public void EndHold()
        {
            EndPull();
            _isHeld = false;
        }

        public bool CanPlace(Vector3 rootPosition, LayerMask obstacleMask)
        {
            Quaternion rotation = Root.rotation;
            return !Physics.CheckBox(rootPosition + rotation * boundsOffset, halfExtents * PLACE_CHECK_SCALE,
                rotation, obstacleMask, QueryTriggerInteraction.Ignore);
        }
        
        public float GetReach(Vector3 direction) 
        {
            Vector3 local = Quaternion.Inverse(Root.rotation) * direction.normalized;
            return Vector3.Dot(boundsOffset, local)
                   + Mathf.Abs(local.x) * halfExtents.x
                   + Mathf.Abs(local.y) * halfExtents.y
                   + Mathf.Abs(local.z) * halfExtents.z;
        }
    }
}