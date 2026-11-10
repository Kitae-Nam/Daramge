using System;
using System.Collections.Generic;

namespace CHG._02_Scripts.Player.Equipment
{
    public interface IEquipmentModule
    {
        event Action<EquipmentPart> OnEquipmentChanged; 
        IReadOnlyCollection<EquipmentSO> EquippedItems { get; } 
        EquipmentSO GetEquipment(EquipmentPart part); 
        void Equip(EquipmentSO equipment); 
        void Unequip(EquipmentPart part); 
    }
}