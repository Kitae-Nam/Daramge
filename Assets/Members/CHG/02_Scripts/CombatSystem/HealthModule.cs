using System;
using GGMLib.ModuleSystems;
using Members.CHG._02_Scripts.Agent.StatSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.CombatSystem
{
    public class HealthModule : MonoBehaviour, IModule
    {
        public event Action OnDeath;
        
        [SerializeField] private StatSO vitalStat;
        [SerializeField] private float baseMaxHealth;
        [SerializeField] private float maxHealth; //나중에 스탯으로 변경한다.
        [SerializeField] private float currentHealth;

        private ModuleOwner _owner;
        private IStatModule _statModule;
        
        public event Action<float, float> OnHealthChanged;
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        
        public void Initialize(ModuleOwner owner)
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
            float k = 0.01f;
            float beforeMaxHealth = maxHealth;
            maxHealth = baseMaxHealth + 5 * currentValue / (1 + k * currentValue);
            float delta = maxHealth - beforeMaxHealth;
            currentHealth = Mathf.Clamp(currentHealth + delta, 1, maxHealth);
            RaiseHealthChanged();
        }

        public void ApplyDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            RaiseHealthChanged();
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnDeath?.Invoke();
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