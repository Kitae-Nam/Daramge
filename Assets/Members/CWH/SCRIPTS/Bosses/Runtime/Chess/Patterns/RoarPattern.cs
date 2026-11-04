using System;
using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns
{
    /// <summary>
    /// 포효 (로어) 패턴 스크립트입니다.
    /// </summary>
    [RequireComponent(typeof(BossPatternRunner), typeof(BossTarget))]
    public class RoarPattern : BossPattern
    {
        [SerializeField] private BossTelegraphPresenter _telegraph;
        [SerializeField] private AreaTelegraphPresenter _areaTelegraph;
        [SerializeField, Min(0f)] private float _roarRadius = 5f;
        [SerializeField, Min(0f)] private float _knockbackSpeed = 10f;
        [SerializeField, Min(0f)] private float _knockbackDuration = 0.4f;

        [SerializeField, Tooltip("IRoarTuningSource 구현체. 비우면 기본 설정을 사용한다.")]
        private MonoBehaviour _tuningSource;


        private BossPatternRunner runner;
        private BossTarget _target;
        private BossGroggyController _groggy;
        private BossCombatPresentation _presentation;
        private IRoarTuningSource _profiles;
        private bool _prepared;


        private RoarTuning Tuning => _profiles != null
            ? _profiles.GetRoar(_roarRadius, _knockbackSpeed, _knockbackDuration)
            : RoarTuning.Normal(_roarRadius, _knockbackSpeed, _knockbackDuration);

        private float Radius => Tuning.Radius;
        public override bool CanExecute() => base.CanExecute() && _target.HasTarget && (_prepared || IsTargetInRange());
        public override float GetSelectionScore(float targetDistance) => targetDistance <= Radius ? 35f : 0f;

        private void Awake()
        {
            runner = GetComponent<BossPatternRunner>();
            _target = GetComponent<BossTarget>();
            _groggy = GetComponent<BossGroggyController>();
            _profiles = _tuningSource as IRoarTuningSource ?? GetComponent<ChessCombatProfiles>();
            _presentation = GetComponent<BossCombatPresentation>();
        }

        public override void BeginWarning()
        {
            _prepared = true;
            if (_areaTelegraph != null) _areaTelegraph.Show(transform.position, Vector3.forward, Radius);
            else if (_telegraph != null)
            {
                _telegraph.SetProgress(0f);
                _telegraph.SetOpacity(0.7f);
            }

            if (_presentation != null) _presentation?.SetRoarCharge(0f);
            base.BeginWarning();
        }

        public override void EndWarning()
        {
            if (_areaTelegraph != null) _areaTelegraph.Hide();
            if (_telegraph != null) _telegraph.SetOpacity(0f);
            if (_presentation != null) _presentation?.SetRoarCharge(0f);

            base.EndWarning();
        }

        public override IEnumerator Execute()
        {
            IKnockbackReceiver receiver =
                _target.HasTarget ? _target.Target.GetComponentInParent<IKnockbackReceiver>() : null;
            if (IsTargetInRange() && receiver != null)
            {
                Vector3 direction = _target.Target.position - transform.position;
                direction.y = 0f;

                RoarTuning tuning = Tuning;
                float duration = tuning.Duration;
                float speed = tuning.Speed;
                if (tuning.WallDamage > 0f && receiver is Component receiverComponent)
                {
                    Rigidbody body = receiverComponent.GetComponentInParent<Rigidbody>();
                    GameObject root = body != null ? body.gameObject : receiverComponent.gameObject;
                    RoarWallImpact impact = root.GetComponent<RoarWallImpact>();
                    if (impact == null) impact = root.AddComponent<RoarWallImpact>();
                    impact.Arm(transform, direction, duration,
                        BossAttackDamage.Resolve(transform, tuning.WallDamage), tuning.WallLayers);
                }

                receiver.ReceiveKnockBack(direction, speed, duration);
            }

            _presentation?.PlayRoarImpact();
            // 1페이즈 포효는 피해 없이 밀치고, 명중 여부와 관계없이 빈틈을 제공한다.
            _groggy.TryEnterGroggy();
            yield break;
        }

        public override void UpdateWarningProgress(float progress)
        {
            if (_areaTelegraph != null) _areaTelegraph.SetProgress(progress);
            if (_telegraph != null) _telegraph.SetProgress(progress);
            if (_presentation != null) _presentation.SetRoarCharge(progress);
        }

        public override void Cleanup()
        {
            _prepared = false;
            if (_areaTelegraph != null) _areaTelegraph.Hide();
            if (_telegraph != null) _telegraph.SetOpacity(0f);
            if (_presentation != null) _presentation.SetRoarCharge(0f);
        }

        private void OnDisable()
        {
            if (runner != null && runner.CurrentPattern == this) runner.CancelCurrentPattern();
        }

        private bool IsTargetInRange()
        {
            if (!_target.HasTarget)
                return false;

            Vector3 pos = _target.Target.position - transform.position;
            pos.y = 0;
            if (pos.sqrMagnitude <= Radius * Radius)
                return true;
            return false;
        }

#if UNITY_EDITOR
        [ContextMenu("테스트 / 로어 패턴 요청")]
        public void RoarPatternEditor()
        {
            if (Application.isPlaying) runner.TryRun(this);
        }

        // 실제 판정과 같은 중심·반지름으로 수평 원을 표시한다.
        private void OnDrawGizmosSelected()
        {
            UnityEditor.Handles.color = Color.yellow;

            UnityEditor.Handles.DrawWireDisc(
                transform.position,
                Vector3.up,
                Mathf.Max(0f, _roarRadius));
        }
#endif
    }
}
