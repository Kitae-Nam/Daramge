using System.Collections;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>각 타격 전에 경고를 다시 보여 주는 2연격. 두 번째 타격도 첫 조준 방향을 유지한다.</summary>
    [RequireComponent(typeof(BossPatternRunner), typeof(BossTarget))]
    public sealed class MeleeComboPattern : BossPattern
    {
        [SerializeField] private AreaTelegraphPresenter _telegraph;
        [SerializeField, Min(0.1f)] private float _range = 9f;
        [SerializeField, Range(1f, 360f)] private float _angle = 140f;
        [SerializeField, Min(0f)] private float _damage = 20f;
        [SerializeField, Min(0.1f)] private float _secondWarning = 0.7f;
        [SerializeField, Min(0.1f)] private float _swingDuration = 0.35f;
        [SerializeField] private LayerMask _hitLayers = ~0;
        private BossTarget _target;
        private BossPatternRunner _runner;
        private BossCombatPresentation _presentation;
        private Vector3 _forward;
        private bool _prepared;
        private void Awake()
        {
            _target = GetComponent<BossTarget>();
            _runner = GetComponent<BossPatternRunner>();
            _presentation = GetComponent<BossCombatPresentation>();
        }
        public override bool CanExecute()
        {
            if (!base.CanExecute() || !_target.HasTarget || _telegraph == null) return false;
            if (GetComponent<BossSwordController>()?.HasSword == true) return false;
            // 예고 후 대상이 멀어져도 저장된 범위에 휘두른다. 회피가 공격 취소로 바뀌지 않게 한다.
            if (_prepared) return true;
            Vector3 offset = _target.Target.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _range * _range;
        }
        public override void BeginWarning()
        {
            _forward = _target.Target.position - transform.position;
            _forward.y = 0f;
            if (_forward.sqrMagnitude < 0.0001f) _forward = transform.forward;
            _forward.Normalize();
            _prepared = true;
            transform.rotation = Quaternion.LookRotation(_forward);
            ShowWarning();
            base.BeginWarning();
        }
        private void ShowWarning() => _telegraph.Show(transform.position, _forward, _range, _angle);
        public override void UpdateWarningProgress(float progress) => _telegraph.SetProgress(progress);
        public override void EndWarning() { _telegraph.Hide(); base.EndWarning(); }
        public override IEnumerator Execute()
        {
            for (int strike = 0; strike < 2; strike++)
            {
                if (!_target.HasTarget) yield break;
                if (strike > 0)
                {
                    // 한 패턴 안의 두 번째 공격도 피할 시간을 보장한다.
                    ShowWarning();
                    for (float elapsed = 0f; elapsed < _secondWarning; elapsed += Time.deltaTime)
                    { _telegraph.SetProgress(elapsed / _secondWarning); yield return null; }
                    _telegraph.Hide();
                }
                if (_presentation != null) _presentation.PlayImpact();
                BossAttackQuery.ApplySector(transform, transform.position, _forward, _range, _angle, 7f,
                    _damage, _hitLayers, targetRoot: _target.HasTarget ? _target.Target : null);
                for (float elapsed = 0f; elapsed < _swingDuration; elapsed += Time.deltaTime)
                { if (_presentation != null) _presentation.SetSwing(elapsed / _swingDuration, strike); yield return null; }
            }
        }
        public override void Cleanup() { _prepared = false; if (_telegraph != null) _telegraph.Hide(); if (_presentation != null) _presentation.ResetWeapon(); }
        private void OnDisable() { if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern(); }
#if UNITY_EDITOR
        [ContextMenu("테스트/2연격 요청")]
        private void TestRun() { if (Application.isPlaying) _runner.TryRun(this); }
#endif
    }
}
