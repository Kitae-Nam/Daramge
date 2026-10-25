using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
    public class RayDamageCaster : AbstractDamageCaster
    {
        public enum CastType {Ray, Sphere, Box}
        
        [SerializeField] private CastType castType = CastType.Sphere;
        
        [SerializeField] private Vector3 boxSize = Vector3.one;
        [SerializeField, Range(0.5f, 3f)] private float castRadius = 1f;
        [SerializeField, Range(0, 1f)] private float casterInterpolation = 0.5f;
        [SerializeField, Range(0, 5f)] private float castingRange = 1f;

        [SerializeField] private bool isDebug;
        
        public override bool CastDamage(Vector3 position, Vector3 direction, SkillDataSO skillData)
        {
            Vector3 startPosition = position - direction * casterInterpolation * 2f;
            
            RaycastHit hit = default;

            bool isHit = castType switch
            {
                CastType.Ray => Physics.Raycast(startPosition, direction, out hit, castingRange, whatIsEnemy),
                CastType.Sphere => Physics.SphereCast(startPosition, castRadius, direction,
                    out hit, castingRange, whatIsEnemy),
                CastType.Box => Physics.BoxCast(startPosition, boxSize * 0.5f, direction,
                    out hit, Quaternion.identity, castingRange, whatIsEnemy),
                _ => false
            };

            if (isHit && hit.collider != null && hit.collider.TryGetComponent(out IDamageable damageable))
            {
                DamageData damageData = StatModule?.CalculateDamage(skillData) ?? new DamageData
                {
                    Attacker = CasterOwner, IsCritical = false, DamageAmount = skillData.baseDamage
                };
                
                Debug.Log($"<color=red>Hit</color> {hit.collider.name} : {damageData.DamageAmount}");
                
                LastHitCritical = damageData.IsCritical;
                LastHitPosition = damageData.HitPoint = hit.point;
                LastHitNormal = damageData.HitNormal = hit.normal;
                
                damageable.ApplyDamage(damageData);
            }
            
            return isHit;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(!isDebug) return;
            Vector3 startPos = transform.position + transform.forward * -casterInterpolation * 2;
            switch (castType)
            {
                case CastType.Ray:
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(startPos, transform.forward * castingRange);
                    break;
                case CastType.Sphere:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(startPos, castRadius);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(startPos + transform.forward * castingRange, castRadius);
                    break;
                case CastType.Box:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(startPos, boxSize);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(startPos + transform.forward * castingRange, boxSize);
                    break;
            }
            Gizmos.color = Color.white;
        }
#endif

    }
}