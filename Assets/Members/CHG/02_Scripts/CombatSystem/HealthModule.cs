using System;
using GGMLib.ModuleSystems;
using Members.CHG._02_Scripts.Agent.StatSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.CombatSystem
{
    public class HealthModule : MonoBehaviour, IModule
    {
        public event Action OnDeath;
        
        [SerializeField] private int baseMaxHealth;
        [SerializeField] private int maxHealth; //나중에 스탯으로 변경한다.
        [SerializeField] private int currentHealth;

        private ModuleOwner _owner;
        
        public event Action<float, float> OnHealthChanged;
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            currentHealth = maxHealth;
        }
        
        public void AfterInit()
        {
        }

        private void Start()
        {
            RaiseHealthChanged();
        }

        private void OnDestroy()
        {
        }

        private void HandleVitalChange(StatSO stat, float currentValue, float prevValue)
        {
            
            RaiseHealthChanged();
        }

        public void ApplyDamage(float damageAmount)
        {
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