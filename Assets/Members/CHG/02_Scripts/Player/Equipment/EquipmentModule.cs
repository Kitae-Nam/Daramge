using System;
using System.Collections.Generic;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    public class EquipmentModule : MonoBehaviour, IModule, IEquipmentModule
    {
        public event Action<EquipmentPart> OnEquipmentChanged;
        
        private IStatModule _statModule;
        private readonly Dictionary<EquipmentPart, EquipmentSO> _equipmentByPart = new();

        public IReadOnlyCollection<EquipmentSO> EquippedItems => _equipmentByPart.Values;
        

        public void Initialize(ModuleOwner owner)
        {
            _statModule = owner.GetModule<IStatModule>();
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
        }

        public EquipmentSO GetEquipment(EquipmentPart part)
            => _equipmentByPart.GetValueOrDefault(part);
        
        public void Equip(EquipmentSO equipment)
        {
            if (equipment == null) return;

            EquipmentSO prev = GetEquipment(equipment.Part);
            if (prev != null) return;

            if (prev != null)
                _statModule.RemoveBonuses(prev, prev.Bonuses);
            
            _equipmentByPart[equipment.Part] = equipment;
            _statModule.AddBonuses(equipment, equipment.Bonuses);
            OnEquipmentChanged?.Invoke(equipment.Part);
        }

        public void Unequip(EquipmentPart part)
        {
            if (!_equipmentByPart.Remove(part, out EquipmentSO prev)) return;

            _statModule.RemoveBonuses(prev, prev.Bonuses);
            OnEquipmentChanged?.Invoke(part);
        }
    }
}