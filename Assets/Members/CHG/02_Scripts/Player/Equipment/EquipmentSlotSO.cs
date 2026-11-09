using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    [CreateAssetMenu(fileName = "Equipment slot", menuName = "Agent/Equipment/Slot", order = 0)]
    public class EquipmentSlotSO : ScriptableObject
    {
        [field:SerializeField] public EquipmentPart Part { get; private set; }
    }
}