using System;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    [Serializable]
    [AddComponentMenu("")]
    [FeedbackPath("Time/Hit Stop")]
    [FeedbackHelp("TimeScaleController를 통해 HitStop을 요청합니다. 씬에 HitStopController가 있어야합니다.")]
    public class MMF_HitStop : MMF_Feedback
    {
        [MMFInspectorGroup("Hit stop", true, 12)]
        [SerializeField] private float duration = 0.06f;
        [SerializeField, Range(0f, 1f)] private float timeScale = 0f;

        public override float FeedbackDuration
        {
            get => ApplyTimeMultiplier(duration);
            set => duration = value;
        }

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (!Active) return;

            float stopDuration = FeedbackDuration * ComputeIntensity(feedbacksIntensity, position);
            EventBus<HitStopEvent>.Raise(new HitStopEvent(stopDuration, timeScale));
        }
    }
}