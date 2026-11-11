using System;
using CHG._02_Scripts.Agent.StatSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Equipment
{
    [Serializable]
    public class EquipmentSetTier
    {
        [field: SerializeField, Min(1)] public int RequiredCount { get; private set; }
        [field: SerializeField] public StatBonus[] Bonuses { get; private set; }
    }
}