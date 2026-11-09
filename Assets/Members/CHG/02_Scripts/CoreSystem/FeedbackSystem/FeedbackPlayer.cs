using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem
{
    public class FeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private MMF_Player feelPlayer;
        [SerializeField, Range(0f, 1f)] private float minIntensity = 0.5f;
        
        private IFeedback[] _feedbacks;

        private void Awake() => _feedbacks = GetComponentsInChildren<IFeedback>();

        public void PlayFeedback(FeedbackContext context)
        {
            foreach (IFeedback feedback in _feedbacks)
                feedback.PlayFeedback(context);
            
            if (feelPlayer != null)
                feelPlayer.PlayFeedbacks(context.HitPoint, Mathf.Lerp(minIntensity, 1f, context.Intensity));
        }
    }
}