using System;
using GGMLib.ModuleSystems;
using Members.CHG._02_Scripts.Agent.StatSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player
{
    public class StaminaModule : MonoBehaviour, IModule
    {
        [SerializeField] private StatSO staminaStat;
        [SerializeField] private float staminaHeal;
        [SerializeField] private float baseMaxStamina;
        [SerializeField] private float maxStamina;
        [SerializeField] private float currentStamina;

        private ModuleOwner _owner;
        private IStatModule _statModule;
        
        public event Action<float, float> OnStaminaChanged;

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            currentStamina = maxStamina;
            _statModule = _owner.GetModule<IStatModule>();
        }   
        
        public void AfterInit()
        {
            if (_statModule != null)
            {
                float stamina = _statModule.SubscribeStat(staminaStat.AssetIndex, HandleStaminaChange, staminaStat.BaseValue);
                currentStamina = maxStamina = baseMaxStamina + stamina;
            }
        }

        private void Start()
        {
            // RaiseHealthChanged();
        }

        private void OnDestroy()
        {
            _statModule?.UnSubscribeStat(staminaStat.AssetIndex, HandleStaminaChange);
        }

        private void HandleStaminaChange(StatSO stat, float currentValue, float prevValue)
        {
            float beforeMaxHealth = maxStamina;
            maxStamina = baseMaxStamina + currentValue;
            float delta = maxStamina - beforeMaxHealth;
            currentStamina = Mathf.Clamp(currentStamina + delta, 1, maxStamina);
            // RaiseHealthChanged();
        }

        public bool TryUseStamina(float useAmount)
        {
            if (CanUseStamina(useAmount))
            {
                currentStamina -= useAmount;
                // RaiseHealthChanged();
                return true;
            }

            return false;
        }

        public bool CanUseStamina(float useAmount)
        {
            if (currentStamina < useAmount)
            {
                return false;
            }

            return true;
        }

        private void RaiseStaminaChanged()
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        
    }
}