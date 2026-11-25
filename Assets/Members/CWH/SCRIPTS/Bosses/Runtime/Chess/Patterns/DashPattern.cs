using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>
    /// 직선 돌진의 순서만 조율한다. 이동/경고 배치/명중은 각각의 컴포넌트에 맡긴다.
    /// 돌진마다 경고 시작 시 방향을 저장하므로 예고한 뒤 플레이어가 움직여도 경로를 유지한다.
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
        [SerializeField, Min(0f), Tooltip("페이즈 컨트롤러가 없는 독립 실행용 기본 피해. 보스 전투는 BossAttackDamage 규칙을 사용한다.")]
        private float _damage = 1f;
        [SerializeField, Min(0f)] private float _knockbackSpeed = 12f;
        [SerializeField, Min(0f)] private float _knockbackDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _warningOpacity = 0.7f;
        [SerializeField, Min(0f)] private float _preferredMinimumDistance = 12f;
        [SerializeField, Min(0.01f)] private float _repeatWarningDuration = 0.35f;
        [SerializeField, Min(0f), Tooltip("설정하면 연속 대쉬의 세 경고에 동일한 시간을 적용합니다. 0이면 기본 경고 시간을 사용합니다.")]
        private float _comboWarningDuration;
        [SerializeField, Min(0f)] private float _comboRecoveryDuration;
        [SerializeField] private bool _showBoardWarnings;
        private BossPatternRunner _runner;
        private BossTarget _target;
        private BossDashMotor _motor;
        private DashHitbox _hitbox;
        private DashPath _path;
        private bool _prepared;
        private bool _warningVisible;
        private bool _finishedCombo;
        private BossCombatPresentation _presentation;
        private ChessBoardGrid _warningBoard;
        [SerializeField, Tooltip("IDashTuningSource 구현체. 비우면 기본 설정을 사용한다.")]
        private MonoBehaviour _tuningSource;
        private IDashTuningSource _profiles;

        public DashPath PlannedPath => _path;
        private DashTuning Tuning => _profiles?.GetDash() ?? DashTuning.Normal;

        // 경고가 표시되는 동안 회피 시간을 주어 예고가 사라진 뒤 출발 시점을 추측하지 않게 한다.
        public override float WarningDuration => Tuning.Count > 1 && _comboWarningDuration > 0f
            ? Mathf.Max(0.01f, _comboWarningDuration)
            : base.WarningDuration;
        // 마지막 돌진을 완료한 경우에만 반격 시간을 늘린다. 러너가 기존 후딜 상태를 유지한다.
        public override float RecoveryDuration => _finishedCombo
            ? Mathf.Max(base.RecoveryDuration, _comboRecoveryDuration)
            : base.RecoveryDuration;
        public override float GetSelectionScore(float targetDistance) => targetDistance >= _preferredMinimumDistance ? 100f : 10f;

        private void Awake()
        {
            _runner = GetComponent<BossPatternRunner>();
            _target = GetComponent<BossTarget>();
            _motor = GetComponent<BossDashMotor>();
            _hitbox = GetComponent<DashHitbox>();
            _presentation = GetComponent<BossCombatPresentation>();
            if (_showBoardWarnings) _warningBoard = GetComponent<ChessBoardGrid>();
            _profiles = _tuningSource as IDashTuningSource ?? GetComponent<ChessCombatProfiles>();
        }

        public override bool CanExecute()
        {
            // 준비 후에는 대상의 이동/소멸로 경로를 다시 조준하지 않는다.
            return base.CanExecute() && _telegraph != null && _motor.IsReady
                   && (_prepared || (_target.HasTarget && _dashDistance > 0f && _dashSpeed > 0f && _dashWidth > 0f));
        }

        public override void BeginWarning()
        {
            // 첫 경고를 만들지 못하면 러너에 취소를 요청해 실행과 후딜을 건너뛴다.
            if (!TryBeginWarning()) _runner.CancelCurrentPattern();
        }

        private bool TryBeginWarning()
        {
            _prepared = false;
            if (_target.HasTarget && _motor.TryPlan(_target.Target.position, _dashDistance, _dashWidth, out _path))
                _prepared = _telegraph.Show(_path, _dashWidth, _warningOpacity);

            if (!_prepared) return false;

            if (_warningBoard != null && Tuning.Count > 1)
                _telegraph.ShowBoardCells(_warningBoard, _path, _dashWidth);
            transform.rotation = Quaternion.LookRotation(_path.Direction);
            _presentation?.SetThrust(true);
            _warningVisible = true;
            base.BeginWarning();
            return true;
        }

        public override void UpdateWarningProgress(float progress)
        {
            if (_telegraph != null) _telegraph.SetProgress(progress);
        }

        public override void EndWarning()
        {
            if (_telegraph != null) _telegraph.Hide();
            if (!_warningVisible) return;
            _warningVisible = false;
            base.EndWarning();
        }

        public override IEnumerator Execute()
        {
            DashTuning tuning = Tuning;
            float damage = BossAttackDamage.Resolve(transform, _damage * tuning.DamageMultiplier);
            float speed = _dashSpeed * tuning.SpeedMultiplier;
            float repeatWarningDuration = Mathf.Max(0.01f, _comboWarningDuration > 0f
                ? _comboWarningDuration
                : _repeatWarningDuration);

            for (int index = 0; index < tuning.Count; index++)
            {
                if (index > 0)
                {
                    // 직전 돌진이 끝난 위치에서 다시 조준한다. 후속 예고도 패턴 안에 두어 함께 취소된다.
                    if (_telegraph == null || !TryBeginWarning()) yield break;
                    for (float elapsed = 0f; elapsed < repeatWarningDuration; elapsed += Time.deltaTime)
                    {
                        UpdateWarningProgress(elapsed / repeatWarningDuration);
                        yield return null;
                    }
                    UpdateWarningProgress(1f);
                    EndWarning();
                }

                if (!_prepared || !_motor.Begin(_path)) yield break;
                yield return ExecuteDash(speed, damage);
            }

            if (tuning.Count > 1 && _comboRecoveryDuration > 0f)
            {
                _finishedCombo = true;
                _presentation?.SetDashRecovery(true);
            }
        }

        private IEnumerator ExecuteDash(float speed, float damage)
        {
            // 돌진마다 명중 기록을 초기화해 같은 대상을 한 돌진에 한 번씩 맞힐 수 있다.
            _hitbox.TargetRoot = _target.HasTarget ? _target.Target : null;
            _hitbox.Begin();
            try
            {
                float travelled = 0f;
                // 한 프레임에 멀리 가더라도 전체 이동 구간을 Sweep해 대상을 건너뛰지 않게 한다.
                _hitbox.Sweep(transform.position, transform.position, _path.Direction, _dashWidth,
                    _motor.BodyHeight, damage, _knockbackSpeed, _knockbackDuration);
                while (travelled < _path.Distance && _motor.IsDashing)
                {
                    float step = Mathf.Min(speed * Time.deltaTime, _path.Distance - travelled);
                    if (step <= 0f) { yield return null; continue; }
                    Vector3 before = transform.position;
                    bool canContinue = _motor.Step(_path.Direction, step, _dashWidth);
                    _hitbox.Sweep(before, transform.position, _path.Direction, _dashWidth,
                        _motor.BodyHeight, damage, _knockbackSpeed, _knockbackDuration);
                    travelled += Mathf.Max(0f, Vector3.Dot(transform.position - before, _path.Direction));
                    if (!canContinue) break;
                    yield return null;
                }
            }
            finally
            {
                // 다음 예고나 후딜 중에는 돌진 판정과 이동 제어를 해제한다.
                _hitbox.Stop();
                _motor.Stop();
            }
        }

        public override void Cleanup()
        {
            _prepared = false;
            _finishedCombo = false;
            EndWarning();
            _hitbox.Stop();
            _motor.Stop();
            _presentation?.SetDashRecovery(false);
            _presentation?.ResetWeapon();
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
