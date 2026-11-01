using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>폰의 근접 원형과 룩/비숍의 방향 고정 공격을 같은 부채꼴 규칙으로 표현한다.</summary>
    public sealed class SectorMinionAttack : ChessMinionAttack
    {
        [SerializeField] private bool _quantizeDirection;
        [SerializeField, Min(0.1f)] private float _range = 10f;
        [SerializeField, Range(1f, 360f)] private float _angle = 18f;
        [SerializeField] private float _directionOffset;
        private Vector3 _origin;
        private Vector3 _forward;
        public override float TriggerDistance => _quantizeDirection ? _range : BaseRadius;
        private float Radius => _quantizeDirection ? _range : BaseRadius;
        private float Angle => _quantizeDirection ? _angle : 360f;

        // 기존 종류 기반 프리팹을 불러오는 어댑터만 사용한다. 새 프리팹은 Inspector에서 설정한다.
        public void ConfigureLine(float range, float angle, float directionOffset)
        { _quantizeDirection = true; _range = range; _angle = angle; _directionOffset = directionOffset; }

        protected override bool TryPrepare()
        {
            _origin = transform.position;
            _forward = Vector3.forward;
            if (_quantizeDirection)
            {
                Vector3 offset = Target.position - _origin;
                float angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                float snapped = Mathf.Round((angle - _directionOffset) / 90f) * 90f + _directionOffset;
                _forward = Quaternion.Euler(0f, snapped, 0f) * Vector3.forward;
            }
            return true;
        }
        public override void ShowWarning() => Warning.Show(_origin, _forward, Radius, Angle);
        public override void BeginAttack() { Warning.Hide(); ApplyDamage(_origin, _forward, Radius, Angle); }
        public override bool Tick(float deltaTime) => true;
    }
}
