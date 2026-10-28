using System;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class StaminaModule : MonoBehaviour, IModule, IAfterInitModule
    {
        [SerializeField] private StatSO staminaStat;
        [SerializeField] private float staminaHeal;
        [SerializeField] private float regenDelay = 1f;
        [SerializeField] private float baseMaxStamina;
        [SerializeField] private float maxStamina;
        [SerializeField] private float currentStamina;

        private ModuleOwner _owner;
        private IStatModule _statModule;
        private float _lastUseTime;
        
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
            RaiseStaminaChanged();
        }

        private void OnDestroy()
        {
            _statModule?.UnSubscribeStat(staminaStat.AssetIndex, HandleStaminaChange);
        }

        private void Update()
        {
            if (currentStamina >= maxStamina) return;
            if (Time.time - _lastUseTime < regenDelay) return;
            
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaHeal *  Time.deltaTime);
            RaiseStaminaChanged();
        }

        private void HandleStaminaChange(StatSO stat, float currentValue, float prevValue)
        {
            float beforeMaxStamina = maxStamina;
            maxStamina = baseMaxStamina + currentValue;
            float delta = maxStamina - beforeMaxStamina;
            currentStamina = Mathf.Clamp(currentStamina + delta, 1, maxStamina);
            RaiseStaminaChanged();
        }

        public bool TryUseStamina(float useAmount)
        {
            if (CanUseStamina(useAmount))
            {
                currentStamina -= useAmount;
                _lastUseTime = Time.time;
                RaiseStaminaChanged();
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