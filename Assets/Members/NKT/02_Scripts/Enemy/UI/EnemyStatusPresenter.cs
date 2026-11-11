using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace NKT.Enemy.UI
{
    public class EnemyStatusPresenter : MonoBehaviour, IModule, IAfterInitModule
    {
        [SerializeField] private HealthViewModelSO sharedHealthVM;

        private HealthModule _health;
        
        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();    
        }

        public void AfterInit()
        {
            if (_health == null || sharedHealthVM == null) return;

            _health.OnHealthChanged += HandleHealthChanged;
            sharedHealthVM.Set(_health.CurrentHealth, _health.MaxHealth);
        }

        private void OnDestroy()
        {
            if (_health == null) return;
            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            sharedHealthVM.Set(current, max);
        }
    }
}