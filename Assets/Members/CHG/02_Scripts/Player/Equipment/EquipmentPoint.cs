using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    public class EquipmentPoint : AbstractInteractPoint
    {
        protected override void HandleInteractKeyPressed()
        {
            EventBus<EquipmentWindowRequestEvent>.Raise(new EquipmentWindowRequestEvent());
        }
    }
}