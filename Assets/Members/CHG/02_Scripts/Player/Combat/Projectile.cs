using CHG._02_Scripts.CombatSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class Projectile : AbstractMonoPoolable
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private float radius;
        [SerializeField] private LayerMask whatIsEnemy;
        
        private PoolManagerSO _poolManager;
        private DamageData _damageData;
        private Vector3 _direction;
        private float _spawnTime;
        private bool _isFlying;

        public void Fire(PoolManagerSO poolManager, Vector3 position, Vector3 direction, DamageData data)
        {
            _poolManager = poolManager;
            _damageData = data;
            _direction = direction.normalized;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(_direction));
            _spawnTime = Time.time;
            _isFlying = true;
            
        }

        private void Update()
        {
            if (!_isFlying) return;
            float moveDistance = speed * Time.deltaTime;
            
            if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, moveDistance, whatIsEnemy))
            {
                HitTarget(hit);
                return;
            }
            
            transform.position += _direction * moveDistance;
            if (Time.time >= _spawnTime + lifeTime)
                ReturnToPool();
        }

        private void HitTarget(RaycastHit hit)
        {
            if (hit.collider.TryGetComponent(out IDamageable damageable))
            {
                _damageData.HitPoint = hit.point;
                _damageData.HitNormal = hit.normal;
                damageable.ApplyDamage(_damageData);
            }
            ReturnToPool();

        }

        private void ReturnToPool()
        {
            if (!_isFlying) return;
            _isFlying = false;
            _poolManager.Push(this);
        }

        public override void ResetItem()
        {
            _isFlying = false;
            _direction = Vector3.zero;
            _damageData = default;
        }
        
        
    }
}