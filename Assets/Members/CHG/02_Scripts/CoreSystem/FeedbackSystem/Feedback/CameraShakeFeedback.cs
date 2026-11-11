using System;
using Unity.Cinemachine;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeFeedback : MonoBehaviour, IFeedback
    {
        [SerializeField] private float minForce = 0.1f;
        [SerializeField] private float maxForce = 0.2f;

        private CinemachineImpulseSource _impulseSource;

        private void Awake()
            => _impulseSource = GetComponent<CinemachineImpulseSource>();

        public void PlayFeedback(FeedbackContext context)
        {
            float force = Mathf.Lerp(minForce, maxForce, context.Intensity);
            _impulseSource.GenerateImpulseAtPositionWithVelocity(context.HitPoint, context.HitDirection * force);
            
        }
    }
}