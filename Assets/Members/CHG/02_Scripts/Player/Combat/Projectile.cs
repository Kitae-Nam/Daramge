using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class Projectile : AbstractMonoPoolable
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifeTime = 3f;
        
        private PoolManagerSO _poolManager;
        private Vector3 _direction;
        private float _spawnTime;
        private bool _isFlying;

        public void Fire(PoolManagerSO poolManager, Vector3 position, Vector3 direction)
        {
            _poolManager = poolManager;
            _direction = direction.normalized;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(_direction));
            _spawnTime = Time.time;
            _isFlying = true;
            
        }

        private void Update()
        {
            if (!_isFlying) return;
            
            transform.position += _direction * (speed * Time.deltaTime);
            if (Time.time >= _spawnTime + lifeTime)
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
        }
    }
}