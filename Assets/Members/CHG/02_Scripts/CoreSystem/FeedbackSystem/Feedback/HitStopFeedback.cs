using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    public class HitStopFeedback : MonoBehaviour, IFeedback
    {
        [SerializeField, Range(0f, 1f)] private float timeScale = 0f;
        [SerializeField] private float minDuration = 0.03f;
        [SerializeField] private float maxDuration = 0.06f;

        public void PlayFeedback(FeedbackContext context)
            => EventBus<HitStopEvent>.Raise(new HitStopEvent(Mathf.Lerp(minDuration, maxDuration, context.Intensity), timeScale)); 

    }
}