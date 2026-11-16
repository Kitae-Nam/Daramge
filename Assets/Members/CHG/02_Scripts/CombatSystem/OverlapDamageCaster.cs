using System.Collections.Generic;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
    public class OverlapDamageCaster : AbstractDamageCaster
    {
        public float Radius => radius;
        
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
            => CastDamage(position, skillData, multiplier, radius);

        public bool CastDamage(Vector3 position, SkillDataSO skillData, float multiplier, float castRadius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, castRadius, _hits, whatIsEnemy);
            bool isHit = false;
            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].TryGetComponent(out IDamageable damageable)) continue;
                if (!_hitSet.Add(damageable)) continue;

                DamageData damageData = DamageCalculator?.CalculateDamage(skillData) ?? new DamageData
                {
                    Attacker = CasterOwner, IsCritical = false, DamageAmount = skillData.baseDamage
                };
                damageData.DamageAmount *= multiplier;
                Collider target = _hits[i];
                if (target is MeshCollider mesh && !mesh.convex)
                {
                    if (TryGetMeshHit(target, position, castRadius, out RaycastHit contact))
                    {
                        damageData.HitPoint = contact.point;
                        damageData.HitNormal = contact.normal;
                    }
                    else
                    {
                        damageData.HitPoint = target.bounds.ClosestPoint(position);
                    }
                }
                else
                {
                    damageData.HitPoint = target.ClosestPoint(position);
                }
                damageable.ApplyDamage(damageData);
                RaiseTargetHit(_hits[i]);
                isHit = true;
            }
            return isHit;
        }

        // Start the sphere outside the mesh so PhysX can report a surface contact.
        private bool TryGetMeshHit(Collider target, Vector3 position, float castRadius, out RaycastHit contact)
        {
            Vector3 direction = position - CasterOwner.transform.position;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : CasterOwner.transform.forward;

            Bounds bounds = target.bounds;
            const float tolerance = 0.01f;
            float distance = Vector3.Distance(position, bounds.center) + bounds.extents.magnitude + castRadius + tolerance;
            float nearest = float.PositiveInfinity;
            contact = default;
            foreach (RaycastHit hit in Physics.SphereCastAll(
                position - direction * distance, castRadius, direction, distance + tolerance, 1 << target.gameObject.layer))
            {
                if (hit.collider != target || hit.distance >= nearest ||
                    (hit.point - position).sqrMagnitude > (castRadius + tolerance) * (castRadius + tolerance))
                {
                    continue;
                }

                nearest = hit.distance;
                contact = hit;
            }

            return contact.collider != null;
        }

#if UNITY_EDITOR

        [SerializeField] private bool isDebug = false;
        private void OnDrawGizmos()
        {
            if (!isDebug) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = Color.white;
        }
#endif
    }
}