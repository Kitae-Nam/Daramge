using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    [Serializable]
    [AddComponentMenu("")]
    [FeedbackPath("Particles/Scaled Particles")]
    [FeedbackHelp("Particles Instantiation과 같고, 강도에 따라 파티클 크기가 바뀝니다.")]
    public class MMF_ScaledParticles : MMF_ParticlesInstantiation
    {
        [MMFInspectorGroup("Intensity Scale", true, 70)]
        [SerializeField] private float minScale = 0.6f;
        [SerializeField] private float maxScale = 1.6f;

        private float _intensity = 1f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            _intensity = ComputeIntensity(feedbacksIntensity, position);
            base.CustomPlayFeedback(position, feedbacksIntensity);
        }

        protected override void PositionParticleSystem(ParticleSystem system)
        {
            base.PositionParticleSystem(system);
            if (system != null)
                system.transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, _intensity);
        }
    }
}