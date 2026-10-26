using System;
using System.Collections;
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


        private BossPatternRunner runner;
        private BossTarget _target;
        private BossGroggyController _groggy;

        private void Awake()
        {
            runner = GetComponent<BossPatternRunner>();
            _target = GetComponent<BossTarget>();
            _groggy = GetComponent<BossGroggyController>();
        }

        public override void BeginWarning()
        {
            if (_areaTelegraph != null) _areaTelegraph.Show(transform.position, Vector3.forward, _roarRadius);
            else if (_telegraph != null)
            {
                _telegraph.SetProgress(0f);
                _telegraph.SetOpacity(0.7f);
            }

            base.BeginWarning();
        }

        public override void EndWarning()
        {
            if (_areaTelegraph != null) _areaTelegraph.Hide();
            if (_telegraph != null)
            {
                _telegraph.SetOpacity(0f);
            }

            base.EndWarning();
        }

        public override IEnumerator Execute()
        {
            IKnockbackReceiver receiver = _target.HasTarget ? _target.Target.GetComponentInParent<IKnockbackReceiver>() : null;
            if (IsTargetInRange() && receiver != null)
            {
                Vector3 direction = _target.Target.position - transform.position;
                direction.y = 0f;

                receiver.ReceiveKnockBack(direction, _knockbackSpeed, _knockbackDuration);
            }

            GetComponent<BossCombatPresentation>()?.PlayImpact();
            // 1페이즈 포효는 피해 없이 밀치고, 명중 여부와 관계없이 빈틈을 제공한다.
            _groggy.TryEnterGroggy();
            yield break;
        }

        public override void UpdateWarningProgress(float progress)
        {
            if (_areaTelegraph != null) _areaTelegraph.SetProgress(progress);
            if (_telegraph != null)
            {
                _telegraph.SetProgress(progress);
            }
        }

        public override void Cleanup()
        {
            if (_areaTelegraph != null) _areaTelegraph.Hide();
            if (_telegraph != null) _telegraph.SetOpacity(0f);
        }

        private void OnDisable() { if (runner != null && runner.CurrentPattern == this) runner.CancelCurrentPattern(); }

        private bool IsTargetInRange()
        {
            if (!_target.HasTarget)
                return false;

            Vector3 pos = _target.Target.position - transform.position;
            pos.y = 0;
            if (pos.sqrMagnitude <= _roarRadius * _roarRadius)
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
