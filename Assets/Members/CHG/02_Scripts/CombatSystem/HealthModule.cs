using System;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
   public class HealthModule : AbstractHealthModule, IAfterInitModule
    {
        [SerializeField] private StatSO vitalStat;
        [SerializeField] private float baseMaxHealth;
        [SerializeField] private float maxHealth; 
        [SerializeField] private float currentHealth;

        private ModuleOwner _owner;
        private IStatModule _statModule;
        
        public event Action<float, float> OnHealthChanged;
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        
        public override void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            currentHealth = maxHealth;
            _statModule = _owner.GetModule<IStatModule>();
        }   
        
        public void AfterInit()
        {
            if (_statModule != null)
            {
                float vital = _statModule.SubscribeStat(vitalStat.AssetIndex, HandleVitalChange, vitalStat.BaseValue);

                float k = 0.01f;
                currentHealth = maxHealth = baseMaxHealth + 5 * vital / (1 + k * vital);
            }
        }

        private void Start()
        {
            RaiseHealthChanged();
        }

        private void OnDestroy()
        {
            _statModule?.UnSubscribeStat(vitalStat.AssetIndex, HandleVitalChange);
        }

        private void HandleVitalChange(StatSO stat, float currentValue, float prevValue)
        {
            float beforeMaxHealth = maxHealth;
            maxHealth = baseMaxHealth + currentValue;
            float delta = maxHealth - beforeMaxHealth;
            currentHealth = Mathf.Clamp(currentHealth + delta, 1, maxHealth);
            RaiseHealthChanged();
        }

        public override void ApplyDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            RaiseHealthChanged();
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                RaiseDeath();
            }
        }

        private void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        [ContextMenu("Hit")]
        private void Hit()
        {
            ApplyDamage(5);
        }
    }
}