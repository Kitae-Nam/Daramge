using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>고정된 착지점을 예고하고 점프한다. 기물 종류나 다른 공격의 구현은 알지 못한다.</summary>
    public sealed class JumpMinionAttack : ChessMinionAttack
    {
        [SerializeField, Min(0.1f)] private float _triggerDistance = 8f;
        [SerializeField, Min(0.1f)] private float _jumpRange = 7f;
        [SerializeField, Min(0.01f)] private float _duration = 0.8f;
        [SerializeField, Min(0f)] private float _height = 2.5f;
        private Vector3 _landing;
        private Vector3 _start;
        private float _elapsed;
        private bool _jumping;
        public override float TriggerDistance => _triggerDistance;
        protected override bool TryPrepare()
        {
            Vector3 offset = Target.position - transform.position;
            offset.y = 0f;
            Vector3 desired = transform.position + Vector3.ClampMagnitude(offset, _jumpRange);
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, Agent.areaMask)) return false;
            _landing = hit.position;
            return true;
        }
        public override void ShowWarning() => Warning.Show(_landing, Vector3.forward, BaseRadius);
        public override void BeginAttack()
        {
            _start = transform.position;
            _elapsed = 0f;
            _jumping = true;
            Agent.updatePosition = false;
            Warning.SetProgress(1f);
        }
        public override bool Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            transform.position = Vector3.Lerp(_start, _landing, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * _height);
            if (progress < 1f) return false;
            if (Agent.isActiveAndEnabled && Agent.isOnNavMesh) Agent.Warp(_landing);
            Agent.updatePosition = true;
            _jumping = false;
            ApplyDamage(_landing, Vector3.forward, BaseRadius, 360f);
            return true;
        }
        public override void Cancel()
        {
            if (!_jumping) return;
            _jumping = false;
            Agent.updatePosition = true;
        }
    }
}
