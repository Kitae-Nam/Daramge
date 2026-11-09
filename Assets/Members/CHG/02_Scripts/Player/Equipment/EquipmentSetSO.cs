using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    [CreateAssetMenu(fileName = "Equipment set data", menuName = "Agent/Equipment/Set data", order = 2)]
    public class EquipmentSetSO : ScriptableObject
    {
        [field: SerializeField] public EquipmentSetTier[] Tiers { get; private set; }
    }
}