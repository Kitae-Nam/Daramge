using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>폰/나이트 중 한 종류를 선택해 정확히 두 마리를 소환한다.</summary>
    [RequireComponent(typeof(BossSummonRoster), typeof(BossTarget), typeof(BossPatternRunner))]
    public sealed class SummonPattern : BossPattern
    {
        [SerializeField] private ChessMinion _pawnPrefab;
        [SerializeField] private ChessMinion _knightPrefab;
        [SerializeField] private AreaTelegraphPresenter _firstWarning;
        [SerializeField] private AreaTelegraphPresenter _secondWarning;
        [SerializeField, Min(0f)] private float _maximumSelectionDistance = 16f;
        private BossSummonRoster _roster;
        private BossTarget _target;
        private BossPatternRunner _runner;
        private Vector3[] _points;
        private ChessMinion _chosen;
        public override float GetSelectionScore(float targetDistance) => targetDistance <= _maximumSelectionDistance ? 25f : 0f;
        private void Awake() { _roster = GetComponent<BossSummonRoster>(); _target = GetComponent<BossTarget>(); _runner = GetComponent<BossPatternRunner>(); }
        public override bool CanExecute() => base.CanExecute() && _roster.LivingCount == 0 && _target.HasTarget &&
            _pawnPrefab != null && _knightPrefab != null && _firstWarning != null && _secondWarning != null;
        public override void BeginWarning()
        {
            if (!_roster.TryGetSpawnPoints(out _points)) { _runner.CancelCurrentPattern(); return; }
            _chosen = Random.value < 0.5f ? _pawnPrefab : _knightPrefab;
            _firstWarning.Show(_points[0], Vector3.forward, 1.6f);
            _secondWarning.Show(_points[1], Vector3.forward, 1.6f);
            base.BeginWarning();
        }
        public override void UpdateWarningProgress(float progress) { _firstWarning.SetProgress(progress); _secondWarning.SetProgress(progress); }
        public override void EndWarning() { _firstWarning.Hide(); _secondWarning.Hide(); base.EndWarning(); }
        public override IEnumerator Execute()
        {
            if (_points == null || _points.Length != 2 || !_target.HasTarget || _roster.LivingCount > 0) yield break;
            for (int i = 0; i < 2; i++)
            {
                ChessMinion minion = Instantiate(_chosen, _points[i], Quaternion.identity);
                _roster.Register(minion);
                // 둘이 동시에 공격하지 않도록 최초 대기 시간도 다르게 준다.
                minion.Initialize(_target.Target, 2f + i * 1.4f, _runner);
            }
            GetComponent<BossCombatPresentation>()?.PlayImpact();
        }
        public override void Cleanup() { if (_firstWarning != null) _firstWarning.Hide(); if (_secondWarning != null) _secondWarning.Hide(); _points = null; }
        private void OnDisable() { if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern(); }
#if UNITY_EDITOR
        [ContextMenu("테스트/기물 소환 요청")]
        private void TestRun() { if (Application.isPlaying) _runner.TryRun(this); }
#endif
    }
}
