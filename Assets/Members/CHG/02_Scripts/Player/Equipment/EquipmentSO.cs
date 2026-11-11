using CHG._02_Scripts.Agent.StatSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    [CreateAssetMenu(fileName = "Equipment data", menuName = "Agent/Equipment/Equipment data", order = 1)]
    public class EquipmentSO : ScriptableObject
    {
        [field: SerializeField] public EquipmentPart Part { get; private set; }
        [field: SerializeField] public EquipmentSetSO[] Set { get; private set; }
        [field: SerializeField] public StatBonus[] Bonuses { get; private set; }
        
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        
    }
}