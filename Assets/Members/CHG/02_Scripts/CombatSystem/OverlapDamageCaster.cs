using System.Collections.Generic;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
    public class OverlapDamageCaster : AbstractDamageCaster
    {
        [SerializeField] private float radius;
        [SerializeField] private int maxCount;

        private Collider[] _hits;
        private readonly HashSet<IDamageable> _hitSet = new HashSet<IDamageable>();


        public override void InitCaster(ModuleOwner owner)
        {
            base.InitCaster(owner);
            _hits = new Collider[maxCount];
        }

        public void ResetHitCaster() => _hitSet.Clear();



        public override bool CastDamage(Vector3 position, Vector3 direction, SkillDataSO skillData)
        {
            return CastDamage(position, skillData, 1f);
        }

        public bool CastDamage(Vector3 position, SkillDataSO skillData, float multiplier)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, _hits, whatIsEnemy);
            bool isHit = false;
            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].TryGetComponent(out IDamageable damageable)) continue;
                if (!_hitSet.Add(damageable)) continue;

                DamageData damageData = StatModule?.CalculateDamage(skillData) ?? new DamageData
                {
                    Attacker = CasterOwner, IsCritical = false, DamageAmount = skillData.baseDamage
                };
                damageData.DamageAmount *= multiplier;
                damageData.HitPoint = _hits[i].ClosestPoint(position);
                damageable.ApplyDamage(damageData);
                isHit = true;
                Debug.Log(isHit);
            }
            return isHit;
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = Color.white;
        }
#endif
    }
}