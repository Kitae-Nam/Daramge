using System;
using System.Collections;
using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.EffectSystem;
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
        [SerializeField] private LayerMask whatIsObstacle;

        [Header("이펙트")]
        [SerializeField] private PoolItemSO explosionVfx;
        [SerializeField] private ParticleSystem trailVfx;
        [SerializeField] private float maxTrailFadeTime = 2f;

        private Renderer[] _bodyRenderers; //트레일을 뺀 미사일 본체 렌더러
        private Collider[] _colliders;
        private Coroutine _returnRoutine;

        private PoolManagerSO _poolManager;
        private Transform _target;
        private DamageData _damageData;
        private LayerMask _whatIsTarget;
        private float _spawnTime;
        private bool _isFlying;

        private void Awake()
        {
            _colliders = GetComponents<Collider>();
            foreach (Collider col in _colliders)
                col.isTrigger = true;

            List<Renderer> bodyRenderers = new List<Renderer>();
            foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
            {
                if (trailVfx != null && rend.transform.IsChildOf(trailVfx.transform)) continue;
                bodyRenderers.Add(rend);
            }
            _bodyRenderers = bodyRenderers.ToArray();

            if (!TryGetComponent(out Rigidbody rb))
                rb = gameObject.AddComponent<Rigidbody>();

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

            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            SetBodyActive(true);

            if (trailVfx != null)
            {
                trailVfx.Clear(true);
                trailVfx.Play(true);
            }

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
            if (!_isFlying) return;

            int hitMask = _whatIsTarget | whatIsObstacle;
            if ((hitMask & (1 << other.gameObject.layer)) == 0) return;

            if (_damageData.Attacker != null && other.transform.IsChildOf(_damageData.Attacker.transform))
                return; //쏜 적 자신에게는 맞지 않는다.

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

            PlayExplosion();

            SetBodyActive(false);
            if (trailVfx != null)
                trailVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _returnRoutine = StartCoroutine(ReturnAfterTrailFade());
        }

        private IEnumerator ReturnAfterTrailFade()
        {
            float elapsed = 0f;
            while (trailVfx != null && trailVfx.IsAlive(true) && elapsed < maxTrailFadeTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _returnRoutine = null;
            _poolManager.Push(this);
        }

        private void SetBodyActive(bool active)
        {
            foreach (Renderer rend in _bodyRenderers)
                rend.enabled = active;
            foreach (Collider col in _colliders)
                col.enabled = active;
        }

        private void PlayExplosion()
        {
            if (explosionVfx == null) return;

            PoolableVfx vfx = _poolManager.Pop<PoolableVfx>(explosionVfx);
            if (vfx == null)
            {
                Debug.LogWarning($"{explosionVfx.name}을 꺼내지 못했습니다. 프리팹에 PoolableVfx가 있는지 확인하세요.", this);
                return;
            }

            PoolManagerSO poolManager = _poolManager;
            void HandleVfxEnd(PoolableVfx endedVfx)
            {
                endedVfx.OnVfxEnd -= HandleVfxEnd;
                poolManager.Push(endedVfx);
            }

            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(transform.position, Quaternion.identity);
        }

        public override void ResetItem()
        {
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            _isFlying = false;
            _target = null;
            SetBodyActive(true); //다음에 꺼낼 때 보이도록 되돌린다.
            if (trailVfx != null)
                trailVfx.Clear(true);
        }
    }
}
