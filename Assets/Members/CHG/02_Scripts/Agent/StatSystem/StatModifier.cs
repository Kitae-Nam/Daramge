using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    public readonly struct StatModifier
    {
        public readonly float Value;
        public readonly StatModifierType Type;

        public StatModifier(float value, StatModifierType type)
        {
            Value = value;
            Type = type;
        }
    }
}