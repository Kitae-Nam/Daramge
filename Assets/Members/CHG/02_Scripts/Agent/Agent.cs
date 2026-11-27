using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.Events;

namespace CHG._02_Scripts.Agent
{
    public abstract class Agent : ModuleOwner, IDamageable, ITickDamageable
    {
        public AgentStatus Status { get; private set; }
        public bool IsDead { get; protected set; }
        public UnityEvent OnHit;
        public UnityEvent OnDeath;
        
        public AbstractHealthModule Health { get; private set; }
        public ActionDataModule ActionData { get; private set; }

        private readonly Dictionary<AgentStatus, int> _statusCounts = new();
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            Health = GetModule<AbstractHealthModule>();
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
            Debug.Log($"[ApplyDamage] {name} dead:{IsDead} status:{Status} health:{Health}", this); // 추가 (임시)
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
                EventBus<DamageTakenEvent>.Raise(new DamageTakenEvent(this, damageData.DamageAmount, damageData.IsCritical));
            }
            
            OnHit?.Invoke();
        }

        public void ApplyTickDamage(float damageAmount, DamageTextStyle textStyle)
        {
            if (IsDead || Health == null) return;
            if (HasStatus(AgentStatus.Invincible)) return;
            
            Health.ApplyDamage(damageAmount);
            EventBus<DamageTakenEvent>.Raise(new DamageTakenEvent(this, damageAmount, false, textStyle));
        }

        public void Revive()
        {
            IsDead = false;
            Health.ResetHealth();
        }
        
        public bool HasStatus(AgentStatus status) => (Status & status) == status;

        public void AddStatus(AgentStatus status)
        {
            _statusCounts.TryGetValue(status, out int count);
            _statusCounts[status] = count + 1;
            Status |= status;
            Debug.Log($"[AddStatus] {name} {status} -> {count + 1}", this); 
        }

        public void RemoveStatus(AgentStatus status)
        {
            Debug.Log($"[RemoveStatus] {name} {status}", this); // 추가 (임시)
            if (!_statusCounts.TryGetValue(status, out int count) || count == 0) return;
            _statusCounts[status] = --count;
            if (count == 0)
                Status &= ~status;
        }
    }
}
