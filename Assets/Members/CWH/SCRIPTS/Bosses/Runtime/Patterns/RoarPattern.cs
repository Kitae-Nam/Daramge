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
            if (_telegraph != null)
            {
                _telegraph.SetProgress(0f);
                _telegraph.SetOpacity(0.7f);
            }

            base.BeginWarning();
        }

        public override void EndWarning()
        {
            if (_telegraph != null)
            {
                _telegraph.SetOpacity(0f);
            }

            base.EndWarning();
        }

        public override IEnumerator Execute()
        {
            if (IsTargetInRange() &&
                _target.Target.TryGetComponent<IKnockbackReceiver>(out var receiver))
            {
                Vector3 direction = _target.Target.position - transform.position;
                direction.y = 0f;

                receiver.ReceiveKnockBack(direction, _knockbackSpeed, _knockbackDuration);
            }

            if (IsTargetInRange())
                Debug.Log("범위 안에 있음");
            else
                Debug.Log("범위 밖에 있음");

            _groggy.TryEnterGroggy();
            yield break;
        }

        public override void UpdateWarningProgress(float progress)
        {
            if (_telegraph != null)
            {
                _telegraph.SetProgress(progress);
            }
        }

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
            Debug.Log("크아앙");
            runner.TryRun(this);
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