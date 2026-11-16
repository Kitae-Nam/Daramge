using System;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
    public abstract class AbstractDamageCaster : MonoBehaviour
    {

        public event Action<Collider> OnTargetHit;
        
        public ModuleOwner CasterOwner { get; private set; }
        public Vector3 LastHitPosition { get; protected set; }
        public Vector3 LastHitNormal { get; protected set; }
        public bool LastHitCritical { get; protected set; }
        public IDamageCalculator DamageCalculator { get; private set; }
        
        [SerializeField] protected LayerMask whatIsEnemy;
        
        [Obsolete("DamageCalculator로 바꾸기")] 
        public IDamageCalculator StatModule => DamageCalculator; 
        
        public virtual void InitCaster(ModuleOwner owner)
        {
            CasterOwner = owner;
            DamageCalculator = owner.GetModule<IDamageCalculator>();
        }

        public abstract bool CastDamage(Vector3 position, Vector3 direction, SkillDataSO skillData);
        protected void RaiseTargetHit(Collider target) => OnTargetHit?.Invoke(target);
    }
}