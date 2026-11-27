using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>기물을 선택하고 프리팹에 명시된 수만큼 예고한 뒤 소환한다.</summary>
    [RequireComponent(typeof(BossSummonRoster), typeof(BossTarget), typeof(BossPatternRunner))]
    public sealed class SummonPattern : BossPattern
    {
        [SerializeField] private ChessMinion _pawnPrefab;
        [SerializeField] private ChessMinion _knightPrefab;
        [SerializeField] private AreaTelegraphPresenter _firstWarning;
        [SerializeField] private AreaTelegraphPresenter _secondWarning;
        [SerializeField, Min(0f)] private float _maximumSelectionDistance = 16f;
        [SerializeField, Range(1, 2)] private int _minimumPhase = 1;
        private BossSummonRoster _roster;
        private BossTarget _target;
        private BossPatternRunner _runner;
        private Vector3[] _points;
        private ChessMinion _chosen;
        private ChessSummonCatalog _catalog;
        private BossPhaseController _phase;
        public override float GetSelectionScore(float targetDistance) => targetDistance <= _maximumSelectionDistance ? 25f : 0f;
        private void Awake()
        {
            _roster = GetComponent<BossSummonRoster>(); _target = GetComponent<BossTarget>();
            _runner = GetComponent<BossPatternRunner>(); _catalog = GetComponent<ChessSummonCatalog>();
            _phase = GetComponent<BossPhaseController>();
        }
        public override bool CanExecute() => base.CanExecute() && _roster.LivingCount == 0 && _target.HasTarget &&
            _pawnPrefab != null && _knightPrefab != null && _firstWarning != null && _secondWarning != null &&
            (_minimumPhase <= 1 || (_phase != null && _phase.CurrentPhase >= _minimumPhase));
        public override void BeginWarning()
        {
            _chosen = _catalog != null ? _catalog.Select(_pawnPrefab, _knightPrefab)
                : (Random.value < 0.5f ? _pawnPrefab : _knightPrefab);
            if (!_roster.TryGetSpawnPoints(_chosen.SummonCount, out _points)) { _runner.CancelCurrentPattern(); return; }
            _firstWarning.Show(_points[0], Vector3.forward, 1.6f);
            if (_points.Length == 2) _secondWarning.Show(_points[1], Vector3.forward, 1.6f);
            base.BeginWarning();
        }
        public override void UpdateWarningProgress(float progress) { _firstWarning.SetProgress(progress); _secondWarning.SetProgress(progress); }
        public override void EndWarning() { _firstWarning.Hide(); _secondWarning.Hide(); base.EndWarning(); }
        public override IEnumerator Execute()
        {
            if (_points == null || _points.Length == 0 || !_target.HasTarget || _roster.LivingCount > 0) yield break;
            for (int i = 0; i < _points.Length; i++)
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
