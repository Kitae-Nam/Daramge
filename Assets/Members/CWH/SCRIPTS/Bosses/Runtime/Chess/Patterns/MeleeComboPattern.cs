using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
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
        [SerializeField, Min(0f), Tooltip("페이즈 컨트롤러가 없는 독립 실행용 기본 피해. 보스 전투는 BossAttackDamage 규칙을 사용한다.")]
        private float _damage = 1f;
        [SerializeField, Min(0.1f)] private float _secondWarning = 0.7f;
        [SerializeField, Min(0.03f)] private float _swingDuration = 0.15f;
        [SerializeField, Min(0f)] private float _finishHoldDuration = 0.1f;
        [SerializeField] private LayerMask _hitLayers = ~0;


        private BossTarget _target;
        private BossPatternRunner _runner;
        private BossCombatPresentation _presentation;
        private BossSwordInventory _inventory;
        private BossSword _heldSword;
        [SerializeField, Tooltip("IMeleeTuningSource 구현체. 비우면 기본 ChessCombatProfiles를 사용한다.")]
        private MonoBehaviour _tuningSource;
        private IMeleeTuningSource _profiles;
        private SwordSwingMotion _motion;
        private Vector3 _forward;
        private bool _prepared;

        public override float GetSelectionScore(float targetDistance) => targetDistance <= _range ? 50f : 0f;

        private void Awake()
        {
            _target = GetComponent<BossTarget>();
            _runner = GetComponent<BossPatternRunner>();
            _presentation = GetComponent<BossCombatPresentation>();
            _inventory = GetComponent<BossSwordInventory>();
            // 구체 구현 선택은 조립 시 한 번만 하고 공격 실행은 작은 계약에 의존한다.
            _profiles = _tuningSource as IMeleeTuningSource ?? GetComponent<ChessCombatProfiles>();
            _motion = MeleeMotionSource.Resolve(gameObject);
        }

        public override bool CanExecute()
        {
            if (!base.CanExecute() || !_target.HasTarget || _telegraph == null) return false;
            if (_presentation == null || !_presentation.HasWeaponPivot) return false;
            if (_inventory == null) return false;
            // 경고 시작 때 확보한 마지막 검도 인정한다. 조회만으로 새 검을 예약하지 않는다.
            if (_heldSword != null) return _heldSword.State == BossSwordState.Held && _prepared;
            if (!_inventory.HasOrbitingSword) return false;

            // 예고 후 대상이 멀어져도 저장된 범위에 휘두른다. 회피가 공격 취소로 바뀌지 않게 한다.
            if (_prepared) return true;
            Vector3 offset = _target.Target.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= _range * _range;
        }

        public override void BeginWarning()
        {
            if (!_target.HasTarget || !_inventory.TryHoldSword(out _heldSword) || !_presentation.TryAttachSword(_heldSword.transform))
            {
                _runner.CancelCurrentPattern();
                return;
            }
            _forward = _target.Target.position - transform.position;
            _forward.y = 0f;
            if (_forward.sqrMagnitude < 0.0001f) _forward = transform.forward;
            _forward.Normalize();
            _prepared = true;
            transform.rotation = Quaternion.LookRotation(_forward);
            _presentation.BeginCharge(0);
            ShowWarning();
            base.BeginWarning();
        }

        private void ShowWarning() => _telegraph.Show(transform.position, _forward, _range, _angle);
        public override void UpdateWarningProgress(float progress)
        {
            _telegraph.SetProgress(progress);
            if (_heldSword != null) _presentation.SetCharge(progress, 0);
        }

        public override void EndWarning()
        {
            _telegraph.Hide();
            base.EndWarning();
        }

        public override IEnumerator Execute()
        {
            if (!_target.HasTarget || _heldSword == null) yield break;
            MeleeTuning tuning = _profiles != null ? _profiles.GetMelee(_swingDuration, _secondWarning) : MeleeTuning.Normal(_swingDuration, _secondWarning);
            int strikes = tuning.Strikes;
            float duration = tuning.SwingDuration;
            float comboWarning = tuning.ComboWarning;
            float damage = BossAttackDamage.Resolve(transform, _damage * tuning.DamageMultiplier);
            try
            {
                for (int strike = 0; strike < strikes; strike++)
                {
                    if (!_target.HasTarget || _heldSword == null) yield break;
                    if (strike > 0)
                    {
                        // 한 패턴 안의 두 번째 공격도 피할 시간을 보장한다.
                        ShowWarning();
                        _presentation.BeginCharge(strike);
                        for (float elapsed = 0f; elapsed < comboWarning; elapsed += Time.deltaTime)
                        {
                            float progress = elapsed / comboWarning;
                            _telegraph.SetProgress(progress);
                            _presentation.SetCharge(progress, strike);
                            yield return null;
                        }

                        _telegraph.Hide();
                    }
                    _presentation.SetCharge(1f, strike);
                    _presentation.BeginSlash();
                    bool hitApplied = false;
                    for (float elapsed = 0f; ; elapsed += Time.deltaTime)
                    {
                        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.03f, duration));
                        _presentation.SetSwing(progress, strike);
                        // 프레임 간격이 커도 정면 통과를 한 번만 감지해 검보다 먼저 피해를 주지 않는다.
                        if (!hitApplied && _motion.HasCrossedFront(progress))
                        {
                            hitApplied = true;
                            _presentation.PlayImpact();
                            BossAttackQuery.ApplySector(transform, transform.position, _forward, _range, _angle, 7f,
                                damage, _hitLayers, targetRoot: _target.HasTarget ? _target.Target : null);
                        }
                        if (progress >= 1f) break;
                        yield return null;
                    }
                    _presentation.EndSlash();
                    // 끝 자세를 잠깐 남겨 빠른 베기가 순간 이동처럼 보이지 않게 한다.
                    for (float elapsed = 0f; elapsed < _finishHoldDuration; elapsed += Time.deltaTime)
                        yield return null;
                }
            }
            finally
            {
                ReleaseSword();
            }
        }

        /// <summary>
        /// 검의 계층을 복원하고 상태를 돌료 공전 코드에 제어권을 넘깁니다. 
        /// </summary>
        private void ReleaseSword()
        {
            if (_presentation != null) _presentation.DetachSword();
            if (_heldSword != null && !_heldSword.PoseBinding.IsReturnPending) _heldSword.TryReturnFromHand();
            
            _heldSword = null;
        }

        public override void Cleanup()
        {
            _prepared = false;
            if (_telegraph != null) _telegraph.Hide();
            ReleaseSword();
        }

        private void OnDisable()
        {
            if (_runner != null && _runner.CurrentPattern == this) _runner.CancelCurrentPattern();
        }
#if UNITY_EDITOR
        [ContextMenu("테스트/2연격 요청")]
        private void TestRun()
        {
            if (Application.isPlaying) _runner.TryRun(this);
        }
#endif
    }
}
