using System.Collections.Generic;
using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    public class Stat
    {
        public delegate void ValueChangeHandler(Stat stat, float currentValue, float prevValue);
        public event ValueChangeHandler OnValueChanged;

        private readonly Dictionary<object, StatModifier> _modifierByKey = new();
        private float _baseValue;
        
        public StatSO Definition { get; }
        public float Value { get; private set; }

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                _baseValue = Mathf.Clamp(value, Definition.MinValue, Definition.MaxValue);  
                RefreshValue();
            }
        }

        public Stat(StatSO definition, float baseValue)
        {
            Definition = definition;
            _baseValue = Mathf.Clamp(baseValue, definition.MinValue, definition.MaxValue);
            Value = CalculateValue();
        }

        public void AddModifier(object key, float modifier, StatModifierType type = StatModifierType.Flat)
        {
            if (_modifierByKey.TryAdd(key, new StatModifier(modifier, type)))
                RefreshValue();
        }

        public void RemoveModifier(object key)
        {
            if (_modifierByKey.Remove(key))
                RefreshValue();
        }

        private void RefreshValue()
        {
            float prevValue = Value;
            Value = CalculateValue();
            
            if (!Mathf.Approximately(Value, prevValue))
                OnValueChanged?.Invoke(this, Value, prevValue);
        }

        private float CalculateValue()
        {
            float flat = 0f;
            float percent = 0f;

            foreach (StatModifier modifier in _modifierByKey.Values)
            {
                if (modifier.Type == StatModifierType.Flat)
                    flat += modifier.Value;
                else 
                    percent += modifier.Value;
            }
            
            float value = (_baseValue + flat) * (1f + percent);
            return Mathf.Clamp(value, Definition.MinValue, Definition.MaxValue);
        }
    }
}