using System;
using Unity.Cinemachine;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeFeedback : MonoBehaviour, IFeedback
    {
        [SerializeField] private float force = 0.2f;

        private CinemachineImpulseSource _impulseSource;

        private void Awake()
            => _impulseSource = GetComponent<CinemachineImpulseSource>();

        public void PlayFeedback(FeedbackContext context)
            => _impulseSource.GenerateImpulseAtPositionWithVelocity(context.HitPoint, context.HitDirection * force);
    }
}