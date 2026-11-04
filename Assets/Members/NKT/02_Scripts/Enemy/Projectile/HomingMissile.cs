using CHG._02_Scripts.CombatSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace NKT.Enemy.Projectile
{
    //타겟을 향해 회전 속도 제한을 두고 유도하다가, 일정 시간이 지나면 직진하는 유도탄.
    //플레이어나 장애물에 닿으면 터지고 풀로 돌아간다.
    public class HomingMissile : AbstractMonoPoolable
    {
        [Header("이동")]
        [SerializeField] private float speed = 7f;
        [Tooltip("초당 회전 각도. 낮을수록 피하기 쉽다.")]
        [SerializeField] private float turnSpeed = 120f;
        [Tooltip("이 시간이 지나면 유도를 멈추고 직진한다.")]
        [SerializeField] private float homingDuration = 1.5f;
        [SerializeField] private float lifeTime = 4f;

        [Header("충돌")]
        [SerializeField] private float hitRadius = 0.3f;
        [SerializeField] private LayerMask whatIsObstacle;

        [Header("폭발 이펙트 (선택)")]
        [SerializeField] private PoolItemSO explosionVfx;

        private PoolManagerSO _poolManager;
        private Transform _target;
        private DamageData _damageData;
        private LayerMask _whatIsTarget;
        private float _spawnTime;
        private bool _isFlying;
        private readonly Collider[] _hits = new Collider[4];

        public void Fire(PoolManagerSO poolManager, Vector3 position, Vector3 direction,
            Transform target, DamageData damageData, LayerMask whatIsTarget)
        {
            _poolManager = poolManager;
            _target = target;
            _damageData = damageData;
            _whatIsTarget = whatIsTarget;

            direction.y = 0; //쿼터뷰에서 읽기 쉽게 높이는 고정
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));

            _spawnTime = Time.time;
            _isFlying = true;
        }

        private void Update()
        {
            if (!_isFlying) return;

            float elapsed = Time.time - _spawnTime;
            if (_target != null && _target.gameObject.activeInHierarchy && elapsed < homingDuration)
                SteerToTarget();

            transform.position += transform.forward * (speed * Time.deltaTime);
            CheckHit();

            if (_isFlying && elapsed >= lifeTime)
                Explode();
        }

        private void SteerToTarget()
        {
            Vector3 direction = _target.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        private void CheckHit()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, hitRadius, _hits, _whatIsTarget | whatIsObstacle);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (_damageData.Attacker != null && hit.transform.IsChildOf(_damageData.Attacker.transform))
                    continue; //쏜 적 자신에게는 맞지 않는다.

                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    DamageData damage = _damageData;
                    damage.HitPoint = transform.position;
                    damage.HitNormal = -transform.forward;
                    damageable.ApplyDamage(damage);
                }

                Explode(); //플레이어든 벽이든 닿으면 터진다.
                return;
            }
        }

        private void Explode()
        {
            if (!_isFlying) return;
            _isFlying = false;

            if (explosionVfx != null)
            {
                IPoolable vfx = _poolManager.Pop<IPoolable>(explosionVfx);
                if (vfx != null)
                    vfx.GameObject.transform.position = transform.position;
            }

            _poolManager.Push(this);
        }

        public override void ResetItem()
        {
            _isFlying = false;
            _target = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
#endif
    }
}
