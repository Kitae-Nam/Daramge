using GGMLib.ModuleSystems;
using Members.CHG._02_Scripts.CombatSystem;
using UnityEngine.Events;

namespace Members.CHG._02_Scripts.Agent
{
    public abstract class Agent : ModuleOwner, IDamageable
    {
        public AgentStatus Status { get; private set; }
        public bool IsDead { get; protected set; }
        public UnityEvent OnHit;
        public UnityEvent OnDeath;
        
        public HealthModule Health { get; private set; }
        public ActionDataModule ActionData { get; private set; }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            Health = GetModule<HealthModule>();
            ActionData = GetModule<ActionDataModule>();
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            Health.OnDeath += HandleDeath;
            OnHit.AddListener(HandleHitEvent);
        }

        protected virtual void OnDestroy()
        {
            Health.OnDeath -= HandleDeath;   
            OnHit.RemoveListener(HandleHitEvent);
        }

        protected abstract void HandleHitEvent();

        protected virtual void HandleDeath()
        {
            IsDead = true;
            OnDeath?.Invoke();
        }

        public void ApplyDamage(DamageData damageData)
        {
            if(IsDead) return;

            if (HasStatus(AgentStatus.Invincible))
                return;
            
            if (ActionData != null)
            {
                ActionData.HitPoint = damageData.HitPoint;
                ActionData.HitNormal = damageData.HitNormal; 
                ActionData.Attacker = damageData.Attacker;
            }
            
            if (Health != null)
            {
                Health.ApplyDamage(damageData.DamageAmount);
            }
            
            OnHit?.Invoke();
        }

        public bool HasStatus(AgentStatus status) => (Status & status) == status;
        public void AddStatus(AgentStatus status) => Status |= status;
        public void RemoveStatus(AgentStatus status) => Status &= ~status;
    }
}
