using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>
    /// 1페이즈 직선 돌진의 순서만 조율한다. 이동/경고 배치/명중은 각각의 컴포넌트에 맡긴다.
    /// 방향은 BeginWarning에서 한 번만 저장하므로 경고 뒤 플레이어가 움직여도 유도되지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossPatternRunner), typeof(BossTarget), typeof(BossDashMotor))]
    [RequireComponent(typeof(DashHitbox), typeof(BossMovementStateController))]
    public sealed class DashPattern : BossPattern
    {
        [SerializeField] private DashTelegraphPresenter _telegraph;
        [SerializeField, Min(0.1f)] private float _dashDistance = 18f;
        [SerializeField, Min(0.1f)] private float _dashSpeed = 30f;
        [SerializeField, Min(0.1f)] private float _dashWidth = 4f;
        [SerializeField, Min(0f)] private float _damage = 25f;
        [SerializeField, Min(0f)] private float _knockbackSpeed = 12f;
        [SerializeField, Min(0f)] private float _knockbackDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _warningOpacity = 0.7f;
        [SerializeField, Min(0f)] private float _preferredMinimumDistance = 12f;
        private BossPatternRunner _runner;
        private BossTarget _target;
        private BossDashMotor _motor;
        private DashHitbox _hitbox;
        private DashPath _path;
        private bool _prepared;

        public DashPath PlannedPath => _path;
        public override float GetSelectionScore(float targetDistance) => targetDistance >= _preferredMinimumDistance ? 100f : 10f;

        private void Awake()
        {
            _runner = GetComponent<BossPatternRunner>();
            _target = GetComponent<BossTarget>();
            _motor = GetComponent<BossDashMotor>();
            _hitbox = GetComponent<DashHitbox>();
        }

        public override bool CanExecute()
        {
            // 준비 후에는 대상의 이동/소멸로 경로를 다시 조준하지 않는다.
            return base.CanExecute() && _telegraph != null && _motor.IsReady
                   && GetComponent<BossSwordController>()?.HasSword != true
                   && (_prepared || (_target.HasTarget && _dashDistance > 0f && _dashSpeed > 0f && _dashWidth > 0f));
        }

        public override void BeginWarning()
        {
            _prepared = false;
            if (_target.HasTarget && _motor.TryPlan(_target.Target.position, _dashDistance, _dashWidth, out _path))
                _prepared = _telegraph.Show(_path, _dashWidth, _warningOpacity);

            if (!_prepared)
            {
                // 경고를 만들지 못한 공격은 실행하지 않는다. 쿨타임을 다시 초기화하지는 않는다.
                _runner.CancelCurrentPattern();
                return;
            }
            transform.rotation = Quaternion.LookRotation(_path.Direction);
            GetComponent<BossCombatPresentation>()?.SetThrust(true);
            base.BeginWarning();
        }

        public override void UpdateWarningProgress(float progress)
        {
            if (_telegraph != null) _telegraph.SetProgress(progress);
        }

        public override void EndWarning()
        {
            if (_telegraph != null) _telegraph.Hide();
            base.EndWarning();
        }

        public override IEnumerator Execute()
        {
            if (!_prepared || !_motor.Begin(_path)) yield break;
            _hitbox.TargetRoot = _target.HasTarget ? _target.Target : null;
            _hitbox.Begin();
            try
            {
                float travelled = 0f;
                // 한 프레임에 멀리 가더라도 전체 이동 구간을 Sweep해 대상을 건너뛰지 않게 한다.
                _hitbox.Sweep(transform.position, transform.position, _path.Direction, _dashWidth,
                    _motor.BodyHeight, _damage, _knockbackSpeed, _knockbackDuration);
                while (travelled < _path.Distance && _motor.IsDashing)
                {
                    float step = Mathf.Min(_dashSpeed * Time.deltaTime, _path.Distance - travelled);
                    if (step <= 0f) { yield return null; continue; }
                    Vector3 before = transform.position;
                    bool canContinue = _motor.Step(_path.Direction, step, _dashWidth);
                    _hitbox.Sweep(before, transform.position, _path.Direction, _dashWidth,
                        _motor.BodyHeight, _damage, _knockbackSpeed, _knockbackDuration);
                    travelled += Mathf.Max(0f, Vector3.Dot(transform.position - before, _path.Direction));
                    if (!canContinue) break;
                    yield return null;
                }
            }
            finally
            {
                // 후딜이 시작되기 전에 판정을 끈다. Cleanup까지 켜 두면 후딜 중에도 맞을 수 있다.
                _hitbox.Stop();
                _motor.Stop();
            }
        }

        public override void Cleanup()
        {
            _prepared = false;
            if (_telegraph != null) _telegraph.Hide();
            _hitbox.Stop();
            _motor.Stop();
            GetComponent<BossCombatPresentation>()?.ResetWeapon();
        }

        private void OnDisable()
        {
            if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern();
        }

#if UNITY_EDITOR
        [ContextMenu("테스트/대쉬 패턴 요청")]
        private void TestDash()
        {
            if (!Application.isPlaying) return;
            if (!_runner.TryRun(this)) Debug.Log("대쉬 요청 거절: 상태, 대상, 경고 연결 또는 쿨타임을 확인하세요.", this);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 direction = _prepared ? _path.Direction : transform.forward;
            float distance = _prepared ? _path.Distance : _dashDistance;
            Vector3 start = _prepared ? _path.Start : transform.position;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.TRS(start + direction * distance * 0.5f,
                Quaternion.LookRotation(direction), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_dashWidth, 0.05f, distance));
            Gizmos.matrix = previous;
        }
#endif
    }
}
