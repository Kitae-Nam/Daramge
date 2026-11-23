using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>
    /// 한 자루마다 최신 대상 위치에 경고, 준비, 발사, 착지 대기를 반복한다.
    /// 소유권과 비행은 Controller에 맡기고 패턴은 공격 순서만 관리한다.
    /// </summary>
    [RequireComponent(typeof(BossSwordController), typeof(BossTarget), typeof(BossPatternRunner))]
    public sealed class SwordThrowPattern : BossPattern
    {
        [SerializeField] private AreaTelegraphPresenter _telegraph;
        [SerializeField, Min(0.1f)] private float _impactRadius = 2.8f;
        [SerializeField, Min(0f)] private float _maximumSelectionDistance = 24f;
        [SerializeField, Min(0.01f)] private float _repeatWarningDuration = 0.7f;
        [SerializeField, Tooltip("IThrowTuningSource 구현체. 비우면 기본 설정을 사용한다.")]
        private MonoBehaviour _tuningSource;
        private BossSwordController _swords;
        private BossTarget _target;
        private BossPatternRunner _runner;
        private IThrowTuningSource _profiles;
        private Vector3 _destination;
        private ThrownBossSword _launched;
        private int _throwCount;
        private bool _sequenceStarted;
        private bool _warningVisible;

        public override float GetSelectionScore(float targetDistance)
            => targetDistance > _maximumSelectionDistance ? 0f : targetDistance >= 9f ? 60f : 15f;

        private void Awake()
        {
            _swords = GetComponent<BossSwordController>();
            _target = GetComponent<BossTarget>();
            _runner = GetComponent<BossPatternRunner>();
            _profiles = _tuningSource as IThrowTuningSource ?? GetComponent<ChessCombatProfiles>();
        }

        public override bool CanExecute() => base.CanExecute() && _target.HasTarget && _telegraph != null &&
            (_sequenceStarted
                ? _swords.OwnsThrowSequence(this) && _swords.CanLaunchPrepared
                : _swords.CanThrow);

        public override void BeginWarning()
        {
            ThrowTuning tuning = _profiles != null ? _profiles.GetThrow() : ThrowTuning.Normal;
            // 시작 시 본수를 상한으로 정해 자연 회수 때문에 같은 공격이 끝없이 이어지지 않게 한다.
            _throwCount = Mathf.Min(tuning.Count, _swords.AvailableCount);
            if (_throwCount == 0 || !_swords.TryBeginThrowSequence(this))
            {
                _runner.CancelCurrentPattern();
                return;
            }
            _sequenceStarted = true;
            if (!PrepareNext()) _runner.CancelCurrentPattern();
        }

        private bool PrepareNext()
        {
            // 각 경고 시작에 위치를 다시 얻되, 그 한 자루의 경고 중에는 추적하지 않는다.
            if (!_target.HasTarget || !_swords.OwnsThrowSequence(this) ||
                !NavMesh.SamplePosition(_target.Target.position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return false;
            if (_swords.PrepareNextThrow(this) != 1) return false;
            _destination = hit.position;
            _telegraph.Show(_destination, Vector3.forward, _impactRadius);
            _warningVisible = true;
            base.BeginWarning();
            UpdateWarningProgress(0f);
            return true;
        }

        public override void UpdateWarningProgress(float progress)
        {
            _telegraph.SetProgress(progress);
            _swords.UpdateThrowPreparation(progress);
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
            for (int index = 0; index < _throwCount; index++)
            {
                if (!_target.HasTarget || !_swords.OwnsThrowSequence(this)) yield break;
                if (index > 0)
                {
                    if (!PrepareNext()) yield break;
                    float duration = Mathf.Max(0.01f, _repeatWarningDuration);
                    for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                    {
                        if (!_swords.OwnsThrowSequence(this)) yield break;
                        UpdateWarningProgress(elapsed / duration);
                        yield return null;
                    }
                    UpdateWarningProgress(1f);
                    EndWarning();
                }

                var launched = _swords.LaunchPreparedVolley(_target.Target, new[] { _destination }, _impactRadius);
                if (launched.Length == 0) yield break;
                _launched = launched[0];
                _telegraph.Show(_destination, Vector3.forward, _impactRadius);
                _telegraph.SetProgress(1f);
                // 착지까지 경고를 유지한다. 박힌 검의 수명은 기다리지 않고 다음 경고를 시작한다.
                while (_launched != null && _launched.IsFlying) yield return null;
                _telegraph.Hide();
            }
        }

        public override void Cleanup()
        {
            EndWarning();
            _swords.EndThrowSequence(this);
            if (_launched != null && _launched.IsFlying) _launched.Recall();
            _launched = null;
            _sequenceStarted = false;
            _throwCount = 0;
        }

        private void OnDisable()
        {
            if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern();
        }

#if UNITY_EDITOR
        [ContextMenu("테스트/검 투척 요청")]
        private void TestRun() { if (Application.isPlaying) _runner.TryRun(this); }
#endif
    }
}