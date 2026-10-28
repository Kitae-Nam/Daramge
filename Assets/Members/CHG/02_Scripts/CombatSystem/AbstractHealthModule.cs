using System;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
    public abstract class AbstractHealthModule : MonoBehaviour, IModule
    {
        public event Action OnDeath;
        
        public abstract void Initialize(ModuleOwner owner);
        public abstract void ApplyDamage(float damage);
        
        protected void RaiseDeath()  => OnDeath?.Invoke();
    }
}