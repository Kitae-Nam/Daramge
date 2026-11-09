using System;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
   public class HealthModule : AbstractHealthModule, IAfterInitModule
    {
        private const float VITAL_HEALTH_SCALE = 5f; 
        private const float VITAL_DIMINISH_RATE = 0.01f; 
        
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
                float vital = _statModule.SubscribeStat(vitalStat, HandleVitalChange, vitalStat.BaseValue);

                currentHealth = maxHealth = CalculateMaxHealth(vital);                
            }
        }

        private void Start()
        {
            RaiseHealthChanged();
        }

        private void OnDestroy()
        {
            _statModule?.UnSubscribeStat(vitalStat, HandleVitalChange);
        }

        private void HandleVitalChange(Stat stat, float currentValue, float prevValue)
        {
            float beforeMaxHealth = maxHealth;
            maxHealth = CalculateMaxHealth(currentValue);
            float delta = maxHealth - beforeMaxHealth;
            currentHealth = Mathf.Clamp(currentHealth + delta, 1, maxHealth);
            RaiseHealthChanged();
        }

        private float CalculateMaxHealth(float vital)
            => baseMaxHealth + VITAL_HEALTH_SCALE * vital / (1 + VITAL_DIMINISH_RATE * vital);

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

        public override void ResetHealth()
        {
            currentHealth = maxHealth;
            RaiseHealthChanged();
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