using System;
using System.Collections.Generic;
using GGMLib.DatabaseSystem.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    [CreateAssetMenu(fileName = "Stat data", menuName = "Agent/Stat data", order = 15)]
    public class StatSO : IndexedAsset, ICloneable
    {
        public delegate void ValueChangeHandler(StatSO stat, float currentValue, float prevValue);
        public event ValueChangeHandler OnValueChanged;
        
        [field: SerializeField] public string StatName { get; private set; }
        [field: SerializeField] public Sprite StatIcon { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public bool IsPercent { get; private set; }

        [SerializeField] private float baseValue;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;
        [SerializeField, TextArea] private string description;

        private Dictionary<object, float> _modifyValueByKey = new();
        private float _modifiedValue = 0;
        
        public float MaxValue => maxValue;
        public float MinValue => minValue;
        
        public float Value => Mathf.Clamp(baseValue + _modifiedValue, MinValue, MaxValue);
        public bool IsMax => Mathf.Approximately(Value, MaxValue);
        public bool IsMin => Mathf.Approximately(Value, MinValue);

        public float BaseValue
        {
            get => baseValue;
            set
            {
                float prevValue = Value;
                baseValue = Mathf.Clamp(value, MinValue, MaxValue);
                TryInvokeValueChangedEvent(Value, prevValue);
            }
        }

        public void AddModifier(object key, float modifier)
        {
            if (_modifyValueByKey.ContainsKey(key)) return;

            float prevValue = Value;
            _modifiedValue += modifier;
            _modifyValueByKey.Add(key, modifier);
            
            TryInvokeValueChangedEvent(Value, prevValue);
        }

        public void RemoveModifier(object key)
        {
            if (_modifyValueByKey.TryGetValue(key, out float modifier))
            {
                float prevValue = Value;
                _modifiedValue -= modifier;
                _modifyValueByKey.Remove(key);
                
                TryInvokeValueChangedEvent(Value, prevValue);
            }
        }

        public void ClearModifiers()
        {
            float prevValue = Value;
            _modifyValueByKey.Clear();
            _modifiedValue = 0;
            TryInvokeValueChangedEvent(Value, prevValue);
        }
        
        private void TryInvokeValueChangedEvent(float currentValue, float prevValue)
        {
            if (!Mathf.Approximately(currentValue, prevValue))
            {
                OnValueChanged?.Invoke(this, currentValue, prevValue);
            }
        }

        public object Clone()
        {
            return Instantiate(this);
        }
    }
}