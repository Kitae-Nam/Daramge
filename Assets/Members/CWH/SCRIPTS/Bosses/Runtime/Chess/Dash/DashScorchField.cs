using System.Collections.Generic;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Dash;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>실제 대쉬 구간의 피해 주기와 수명만 관리한다. 메시 생성은 표시 컴포넌트가 맡는다.</summary>
    public sealed class DashScorchField : MonoBehaviour
    {
        private Transform _owner;
        private Transform _target;
        private BossCombatGate _gate;
        private BossHealth _health;
        private Vector3 _start;
        private Vector3 _end;
        private float _width;
        private ScorchSettings _settings;
        private float _expiresAt;
        private float _nextDamage;
        private IDashScorchVisual _visual;

        public void Initialize(Transform owner, Transform target, float width, ScorchSettings settings, IDashScorchVisual visual)
        {
            _owner = owner;
            _target = target;
            _gate = owner.GetComponent<BossCombatGate>();
            _health = owner.GetComponent<BossHealth>();
            _start = _end = owner.position;
            _width = width;
            _settings = new ScorchSettings(settings.Lifetime, settings.TickInterval,
                BossAttackDamage.ResolveHazard(owner, settings.Damage));
            _visual = visual;
            _nextDamage = Time.time + settings.TickInterval;
            ExtendTo(owner.position);
        }

        public void ExtendTo(Vector3 end)
        {
            _end = end;
            _expiresAt = Time.time + _settings.Lifetime;
            if (_visual != null) _visual.SetSegment(_start, _end, _width);
        }

        private void Update()
        {
            if (_owner == null || !_owner.gameObject.activeInHierarchy || _health == null || _health.IsDead ||
                (_gate != null && !_gate.IsOpen) || Time.time >= _expiresAt)
            { Destroy(gameObject); return; }
            if (_target == null || Time.time < _nextDamage) return;
            _nextDamage = Time.time + _settings.TickInterval;
            Vector3 delta = _end - _start;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.001f) return;
            Vector3 center = (_start + _end) * 0.5f + Vector3.up * 2f;
            // 대상의 콜라이더 수와 관계없이 같은 주기에는 한 번만 피해를 준다.
            var damaged = new HashSet<IDamageReceiver>();
            foreach (Collider hit in Physics.OverlapBox(center, new Vector3(_width * 0.5f, 2f, delta.magnitude * 0.5f),
                         Quaternion.LookRotation(delta), ~0, QueryTriggerInteraction.Collide))
            {
                if (!hit.transform.IsChildOf(_target)) continue;
                IDamageReceiver receiver = hit.GetComponentInParent<IDamageReceiver>();
                if (receiver != null && damaged.Add(receiver)) receiver.ReceiveDamage(_settings.Damage);
            }
        }
        public void Cancel() { enabled = false; Destroy(gameObject); }
    }
}
