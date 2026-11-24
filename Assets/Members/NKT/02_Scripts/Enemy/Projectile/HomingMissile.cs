using CHG._02_Scripts.CombatSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace NKT.Enemy.Projectile
{
    //타겟을 향해 회전 속도 제한을 두고 유도하다가, 일정 시간이 지나면 직진하는 유도탄.
    //플레이어나 장애물에 닿으면 터지고 풀로 돌아간다.
    //판정은 프리팹의 트리거 콜라이더로 한다.
    [RequireComponent(typeof(Rigidbody))]
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
        [Tooltip("타겟(Fire로 받는 레이어) 외에 닿으면 터지는 레이어. 보통 Wall.")]
        [SerializeField] private LayerMask whatIsObstacle;

        [Header("폭발 이펙트 (선택)")]
        [SerializeField] private PoolItemSO explosionVfx;

        private PoolManagerSO _poolManager;
        private Transform _target;
        private DamageData _damageData;
        private LayerMask _whatIsTarget;
        private float _spawnTime;
        private bool _isFlying;

        private void Awake()
        {
            //판정 콜라이더는 트리거여야 OnTriggerEnter가 온다. (CharacterController와는 일반 충돌 이벤트가 오지 않는다)
            foreach (Collider col in GetComponents<Collider>())
                col.isTrigger = true;

            //RequireComponent는 이미 만든 프리팹에는 자동으로 붙지 않으므로 없으면 붙인다.
            if (!TryGetComponent(out Rigidbody rb))
                rb = gameObject.AddComponent<Rigidbody>();

            //transform으로 직접 움직이므로 물리에 밀리거나 떨어지지 않게 한다.
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

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

            if (elapsed >= lifeTime)
            {
                Explode();
            }
        }

        private void SteerToTarget()
        {
            Vector3 direction = _target.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isFlying) return; //풀에서 꺼낸 직후(Fire 전)나 이미 터진 뒤에 들어오는 충돌은 무시

            int hitMask = _whatIsTarget | whatIsObstacle;
            if ((hitMask & (1 << other.gameObject.layer)) == 0) return;

            if (_damageData.Attacker != null && other.transform.IsChildOf(_damageData.Attacker.transform))
                return; //쏜 적 자신에게는 맞지 않는다.

            //콜라이더가 자식에 있어도 루트의 Agent(IDamageable)를 찾는다.
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                DamageData damage = _damageData;
                damage.HitPoint = other.ClosestPoint(transform.position);
                damage.HitNormal = -transform.forward;
                damageable.ApplyDamage(damage);
            }

            Explode(); //플레이어든 벽이든 닿으면 터진다.
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
    }
}
