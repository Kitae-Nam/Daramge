using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Hold
{
    public class HoldableInteractPoint : AbstractInteractPoint
    {
        private IHoldable _holdable;

        private void Awake()
        {
            _holdable = GetComponentInParent<IHoldable>();
            Debug.Assert(_holdable != null, $"IHoldable is null : {gameObject.name}");
            _holdable.OnHold += HandleHold;
        }

        private void OnDestroy()
        {
            if (_holdable != null)
                _holdable.OnHold -= HandleHold;
                
        }

        private void HandleHold(IHoldable holdable) => ClearInteractors();

        public override bool CanInteract(PlayerController player)
            => _holdable.CanHold && player.GetModule<ICarrier>() != null;

        public override void Interact(PlayerController player)
            => player.GetModule<ICarrier>().RequestPickUp(_holdable);
    }
}