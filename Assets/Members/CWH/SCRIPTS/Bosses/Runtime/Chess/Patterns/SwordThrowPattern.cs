using System.Collections;
using System.Collections.Generic;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>플레이어 위치를 고정해 예고한 뒤 검을 던진다. 검이 남아 있으면 다시 던지지 않는다.</summary>
    [RequireComponent(typeof(BossSwordController), typeof(BossTarget), typeof(BossPatternRunner))]
    public sealed class SwordThrowPattern : BossPattern
    {
        [SerializeField] private AreaTelegraphPresenter _telegraph;
        [SerializeField, Min(0.1f)] private float _impactRadius = 2.8f;
        [SerializeField, Min(0f)] private float _maximumSelectionDistance = 24f;
        private BossSwordController _swords;
        private BossTarget _target;
        private BossPatternRunner _runner;
        private Vector3 _destination;
        private ThrownBossSword _launched;
        private Vector3[] _destinations;
        private ThrownBossSword[] _volley = new ThrownBossSword[0];
        private readonly List<AreaTelegraphPresenter> _extraWarnings = new List<AreaTelegraphPresenter>();
        [SerializeField, Tooltip("IThrowTuningSource 구현체. 비우면 기본 설정을 사용한다.")]
        private MonoBehaviour _tuningSource;
        private IThrowTuningSource _profiles;
        public override float GetSelectionScore(float targetDistance)
            => targetDistance > _maximumSelectionDistance ? 0f : targetDistance >= 9f ? 60f : 15f;
        private void Awake()
        {
            _swords = GetComponent<BossSwordController>();
            _target = GetComponent<BossTarget>();
            _runner = GetComponent<BossPatternRunner>();
            _profiles = _tuningSource as IThrowTuningSource ?? GetComponent<ChessCombatProfiles>();
        }
        public override bool CanExecute() => base.CanExecute() && _swords.CanThrow && _target.HasTarget && _telegraph != null;
        public override void BeginWarning()
        {
            // TryRun 직후의 한 프레임 사이에 대상/검이 사라졌다면 빈 일제 투척을 준비하지 않는다.
            if (!_target.HasTarget || !_swords.CanThrow) { _runner.CancelCurrentPattern(); return; }
            if (!NavMesh.SamplePosition(_target.Target.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            { _runner.CancelCurrentPattern(); return; }
            _destination = hit.position;
            ThrowTuning tuning = _profiles != null ? _profiles.GetThrow() : ThrowTuning.Normal;
            int count = Mathf.Min(tuning.Count, _swords.AvailableCount);
            _destinations = new Vector3[count];
            _destinations[0] = _destination;
            for (int i = 1; i < count; i++)
            {
                float radians = (i - 1) * Mathf.PI * 2f / Mathf.Max(1, count - 1);
                Vector3 desired = _destination + new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * tuning.Spread;
                _destinations[i] = NavMesh.SamplePosition(desired, out NavMeshHit extra, 2f, NavMesh.AllAreas) ? extra.position : _destination;
                Material material = _telegraph.GetComponent<Renderer>().sharedMaterial;
                AreaTelegraphPresenter warning = AreaTelegraphPresenter.Create(transform, material, "Volley warning");
                _extraWarnings.Add(warning);
                warning.Show(_destinations[i], Vector3.forward, _impactRadius);
            }
            _telegraph.Show(_destination, Vector3.forward, _impactRadius);
            base.BeginWarning();
        }
        public override void UpdateWarningProgress(float progress)
        {
            _telegraph.SetProgress(progress);
            foreach (AreaTelegraphPresenter warning in _extraWarnings) warning.SetProgress(progress);
        }
        public override void EndWarning()
        {
            _telegraph.Hide();
            foreach (AreaTelegraphPresenter warning in _extraWarnings) warning.Hide();
            base.EndWarning();
        }
        public override IEnumerator Execute()
        {
            if (!_target.HasTarget) yield break;
            if (_destinations == null || _destinations.Length == 0) yield break;
            _volley = _swords.ThrowVolley(_target.Target, _destinations, _impactRadius);
            _launched = _volley.Length > 0 ? _volley[0] : null;
            // 착지 직전까지 경고를 유지해 공중의 검과 착지 범위를 동시에 읽을 수 있다.
            _telegraph.Show(_destination, Vector3.forward, _impactRadius);
            _telegraph.SetProgress(1f);
            for (int i = 0; i < _extraWarnings.Count; i++)
            {
                _extraWarnings[i].Show(_destinations[i + 1], Vector3.forward, _impactRadius);
                _extraWarnings[i].SetProgress(1f);
            }
            while (System.Array.Exists(_volley, sword => sword != null && sword.IsFlying)) yield return null;
            _telegraph.Hide();
            foreach (AreaTelegraphPresenter warning in _extraWarnings) warning.Hide();
        }
        public override void Cleanup()
        {
            if (_telegraph != null) _telegraph.Hide();
            // 비행 중 중단된 공격만 취소한다. 정상 착지한 검은 별도의 수명을 가진다.
            if (_launched != null && _launched.IsFlying) _launched.Recall();
            foreach (ThrownBossSword sword in _volley)
                if (sword != null && sword.IsFlying) sword.Recall();
            _volley = new ThrownBossSword[0];
            foreach (AreaTelegraphPresenter warning in _extraWarnings)
                if (warning != null) Destroy(warning.gameObject);
            _extraWarnings.Clear();
            _destinations = null;
            _launched = null;
        }
        private void OnDisable() { if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern(); }
#if UNITY_EDITOR
        [ContextMenu("테스트/검 투척 요청")]
        private void TestRun() { if (Application.isPlaying) _runner.TryRun(this); }
#endif
    }
}
