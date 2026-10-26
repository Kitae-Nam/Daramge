using System.Collections;
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
        private BossSwordController _swords;
        private BossTarget _target;
        private BossPatternRunner _runner;
        private Vector3 _destination;
        private ThrownBossSword _launched;
        private void Awake() { _swords = GetComponent<BossSwordController>(); _target = GetComponent<BossTarget>(); _runner = GetComponent<BossPatternRunner>(); }
        public override bool CanExecute() => base.CanExecute() && _swords.CanThrow && _target.HasTarget && _telegraph != null;
        public override void BeginWarning()
        {
            if (!NavMesh.SamplePosition(_target.Target.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            { _runner.CancelCurrentPattern(); return; }
            _destination = hit.position;
            _telegraph.Show(_destination, Vector3.forward, _impactRadius);
            base.BeginWarning();
        }
        public override void UpdateWarningProgress(float progress) => _telegraph.SetProgress(progress);
        public override void EndWarning() { _telegraph.Hide(); base.EndWarning(); }
        public override IEnumerator Execute()
        {
            if (!_target.HasTarget) yield break;
            _launched = _swords.Throw(_target.Target, _destination, _impactRadius);
            // 착지 직전까지 경고를 유지해 공중의 검과 착지 범위를 동시에 읽을 수 있다.
            _telegraph.Show(_destination, Vector3.forward, _impactRadius);
            _telegraph.SetProgress(1f);
            while (_launched != null && _launched.IsFlying) yield return null;
            _telegraph.Hide();
        }
        public override void Cleanup()
        {
            if (_telegraph != null) _telegraph.Hide();
            // 비행 중 중단된 공격만 취소한다. 정상 착지한 검은 별도의 수명을 가진다.
            if (_launched != null && _launched.IsFlying) _launched.Recall();
            _launched = null;
        }
        private void OnDisable() { if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern(); }
#if UNITY_EDITOR
        [ContextMenu("테스트/검 투척 요청")]
        private void TestRun() { if (Application.isPlaying) _runner.TryRun(this); }
#endif
    }
}
