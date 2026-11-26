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
        public float BottomOffset => halfExtents.y - boundsOffset.y;

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
            => !Physics.CheckBox(rootPosition + boundsOffset, halfExtents * PLACE_CHECK_SCALE,
                Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);
    }
}