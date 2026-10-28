using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>검 한 개의 비행→박힘→회수 수명. 파괴 보상은 자연 회수와 구분해 한 번만 전달한다.</summary>
    [RequireComponent(typeof(BreakableHealth))]
    public sealed class ThrownBossSword : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField, Min(0.03f)] private float _flightDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float _embeddedDuration = 7f;
        [SerializeField, Min(0f)] private float _impactDamage = 25f;
        private BreakableHealth _health;
        private Transform _owner;
        private Transform _target;
        private Vector3 _start;
        private Vector3 _destination;
        private float _radius;
        private float _timer;
        private bool _launched;
        private bool _finished;
        public bool IsFlying { get; private set; }
        public bool IsEmbedded => _launched && !IsFlying && !_finished;
        public event Action<ThrownBossSword, bool> Finished;

        private void Awake() { _health = GetComponent<BreakableHealth>(); _health.CanTakeDamage = false; }
        private void OnEnable() => _health.Broken += HandleBroken;
        public void Launch(Transform owner, Transform target, Vector3 start, Vector3 destination, float radius)
        {
            _owner = owner; _target = target; _start = start; _destination = destination; _radius = radius;
            transform.position = start;
            // 모델의 칼끝(+Y)을 비행 방향에 맞춘 뒤 고정한다. 회전 투척/포물선을 사용하지 않는다.
            if (_visual != null && (destination - start).sqrMagnitude > 0.0001f)
                _visual.rotation = Quaternion.FromToRotation(Vector3.up, (destination - start).normalized);
            _timer = 0f; _launched = true; IsFlying = true;
        }
        private void Update()
        {
            if (!_launched || _finished) return;
            _timer += Time.deltaTime;
            if (IsFlying)
            {
                float progress = Mathf.Clamp01(_timer / _flightDuration);
                transform.position = Vector3.Lerp(_start, _destination, progress);
                if (progress < 1f) return;
                IsFlying = false;
                _timer = 0f;
                if (_visual != null) _visual.localRotation = Quaternion.Euler(180f, 0f, 0f);
                _health.CanTakeDamage = true;
                if (_target != null && _owner != null)
                    BossAttackQuery.ApplySector(_owner, _destination, Vector3.forward, _radius, 360f, 5f,
                        _impactDamage, ~0, targetRoot: _target);
            }
            else if (_timer >= _embeddedDuration) Finish(false);
        }
        private void HandleBroken() { if (IsEmbedded) Finish(true); }
        public void Recall() => Finish(false);
        private void Finish(bool broken)
        {
            if (_finished) return;
            _finished = true;
            IsFlying = false;
            _health.CanTakeDamage = false;
            // 소유자가 사라져도 콜백으로 목록과 검 표시를 정리할 수 있게 먼저 통지한다.
            Finished?.Invoke(this, broken);
            Destroy(gameObject);
        }
        private void OnDisable()
        {
            _health.Broken -= HandleBroken;
            if (_launched && !_finished) Finish(false);
        }
    }
}
