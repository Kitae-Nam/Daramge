using System.Collections.Generic;
using System.Linq;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    public class EquipmentSetBonusModule : MonoBehaviour, IModule
    {
        private readonly Dictionary<EquipmentSetSO, int> _countBySet = new();
        private readonly HashSet<EquipmentSetTier> _activeTiers = new();
        private readonly HashSet<EquipmentSetTier> _nextActiveTiers = new();

        private IEquipmentModule _equipmentModule;
        private IStatModule _statModule;

        public void Initialize(ModuleOwner owner)
        {
            _equipmentModule = owner.GetModule<IEquipmentModule>();
            _statModule = owner.GetModule<IStatModule>();
            Debug.Assert(_equipmentModule != null, $"equipmentModule is null : {gameObject.name}");
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");

            _equipmentModule.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void OnDestroy()
        {
            if (_equipmentModule != null)
                _equipmentModule.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void HandleEquipmentChanged(EquipmentPart part)
        {
            CountSetPieces();
            CollectActiveTiers();
            ApplyTierChanges();
        }

        private void CountSetPieces()
        {
            _countBySet.Clear();

            foreach (EquipmentSO equipment in _equipmentModule.EquippedItems)
            {
                foreach (EquipmentSetSO set in equipment.Set.Distinct())
                {
                    if (set == null) continue;
                    
                    _countBySet[set] = _countBySet.GetValueOrDefault(set) + 1;  
                    
                }
                
            }
        }

        private void CollectActiveTiers()
        {
            _nextActiveTiers.Clear();

            foreach (KeyValuePair<EquipmentSetSO, int> setPair in _countBySet)
            {
                foreach (EquipmentSetTier tier in setPair.Key.Tiers)
                {
                    if (setPair.Value >= tier.RequiredCount)
                        _nextActiveTiers.Add(tier);
                }
            }
        }

        private void ApplyTierChanges()
        {
            foreach (EquipmentSetTier tier in _activeTiers)
            {
                if (!_nextActiveTiers.Contains(tier))
                    _statModule.RemoveBonuses(tier, tier.Bonuses);
            }

            foreach (EquipmentSetTier tier in _nextActiveTiers)
            {
                if (!_activeTiers.Contains(tier))
                    _statModule.AddBonuses(tier, tier.Bonuses);
            }

            _activeTiers.Clear();
            _activeTiers.UnionWith(_nextActiveTiers);
        }
    }
}