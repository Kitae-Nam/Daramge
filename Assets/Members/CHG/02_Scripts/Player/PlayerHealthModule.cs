using System;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerHealthModule : AbstractHealthModule
    {
        [SerializeField] private int maxHealth = 6;
        
        private int _currentHealth;
        
        public event Action<int, int> OnHealthChanged;
        
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        
        public override void Initialize(ModuleOwner owner)
        {
            _currentHealth = maxHealth;
            EventBus<MaxHealthIncreaseEvent>.Subscribe(HandleMaxHealthIncrease);
        }

        private void OnDestroy() => EventBus<MaxHealthIncreaseEvent>.Unsubscribe(HandleMaxHealthIncrease);

        private void Start()
        {
            RaiseHealthChanged();
        }

        public override void ApplyDamage(float damageAmount)
        {
            if (_currentHealth <= 0) return;

            int damage = Mathf.Max(1, Mathf.FloorToInt(damageAmount + 0.5f));
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            RaiseHealthChanged();
            
            if (_currentHealth == 0)
                RaiseDeath();
        }

        public override void ResetHealth()
        {
            _currentHealth = maxHealth;
            RaiseHealthChanged();
        }

        private void HandleMaxHealthIncrease(MaxHealthIncreaseEvent evt)
        {
            maxHealth += evt.Amount;
            _currentHealth += evt.Amount;
            RaiseHealthChanged();
        }
        
        private void RaiseHealthChanged() => OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        
        [ContextMenu("Hit")]
        private void Hit() => ApplyDamage(1f);
    }
}