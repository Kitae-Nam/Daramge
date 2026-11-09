using System;
using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    [Serializable]
    public class StatBonus
    {
        [field: SerializeField] public StatSO StatData { get; private set; }
        [field: SerializeField] public StatModifierType Type { get; private set; }
        [field: SerializeField] public float Value { get; private set; }
    }
}