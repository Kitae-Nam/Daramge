using System;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class CrateInteractPoint : AbstractInteractPoint
    {
        private PushableCrate _crate;

        private void Awake()
        {
            _crate = GetComponentInParent<PushableCrate>();
            Debug.Assert(_crate != null, $"PushableCrate is null : {gameObject.name}");
            
        }

        public override bool CanInteract => !_crate.IsHeld && !_crate.IsTransitioning;
        
        public override void Interact(PlayerController player)
        {
            ICarrier carrier = player.GetModule<ICarrier>();
            if (carrier != null)    
                carrier.RequestPickUp(_crate);
        }
    }
}