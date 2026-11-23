using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    public class EquipmentPoint : AbstractInteractPoint
    {
        public override void Interact(PlayerController player)
        {
            EventBus<EquipmentWindowRequestEvent>.Raise(new EquipmentWindowRequestEvent());
        }
    }
}