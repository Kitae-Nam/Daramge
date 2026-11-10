using System;
using System.Text;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    public class EquipmentTester : MonoBehaviour, IModule
    {
        [SerializeField] private EquipmentSlotSO testSlot;
        [SerializeField] private EquipmentSO testEquipment;
        [SerializeField] private StatSO[] logStats;

        private ModuleOwner _owner;
        private IEquipmentModule _equipmentModule;
        private IStatModule _statModule;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _equipmentModule = owner.GetModule<IEquipmentModule>();
            _statModule = owner.GetModule<IStatModule>();
            Debug.Assert(_equipmentModule != null, $"equipmentModule is null : {gameObject.name}");
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
        }

        [ContextMenu("Test Equip")]
        private void TestEquip()
        {
            if (!Application.isPlaying || testSlot == null || testEquipment == null)
            {
                Debug.LogWarning($"testEquipment is null: {gameObject.name}");
                return;
            }

            _equipmentModule.Equip(testEquipment);
            Debug.Log($"장착 : {testEquipment.Part} {testEquipment.name}");
            LogState();
        }

        [ContextMenu("Test Unequip")]
        private void TestUnequip()
        {
            if (!Application.isPlaying || testEquipment == null) return;

            _equipmentModule.Unequip(testEquipment.Part);
            LogState();
        }

        [ContextMenu("Log State")]
        private void LogState()
        {
            if (!Application.isPlaying) return;

            StringBuilder builder = new($"[{_owner.name}] 장착 상태\n");

            foreach (EquipmentPart part in Enum.GetValues(typeof(EquipmentPart)))
            {
                EquipmentSO equipment = _equipmentModule.GetEquipment(part);
                builder.AppendLine($"{part} : {(equipment != null ? equipment.name : "-")}");
            }

            foreach (StatSO statData in logStats)
            {
                Stat stat = statData != null ? _statModule.GetStat(statData) : null;
                if (stat != null)
                    builder.AppendLine($"{statData.name} : {stat.Value}");
            }

            Debug.Log(builder.ToString());
        }
    }
}