using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    [Serializable]
    public class ChargeLogic
    {
        [SerializeField] private float maxChargeTime = 1.5f;
        [SerializeField] private float minChargeTime = 0.2f;
        
        public bool IsCharging { get; private set; }
        public float NormalizedCharge => Mathf.Clamp01(_elapsed / maxChargeTime);
        public bool IsValidCharge => _elapsed >= minChargeTime;
        public event Action<float> OnChargeChanged;

        private float _elapsed;

        public void Begin()
        {
            IsCharging = true;
            _elapsed = 0f;
        }

        public void Tick(float dt)
        {
            if (!IsCharging) return;
            _elapsed = Mathf.Min(_elapsed + dt, maxChargeTime);
            OnChargeChanged?.Invoke(NormalizedCharge);
        }

        public float Release()
        {
            IsCharging = false;
            return NormalizedCharge;
        }
        
        public void Cancel()
        {
            IsCharging = false;
            _elapsed = 0f;
        }
    }
}