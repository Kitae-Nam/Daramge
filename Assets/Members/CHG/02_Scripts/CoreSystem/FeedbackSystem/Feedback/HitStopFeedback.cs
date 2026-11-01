using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    public class HitStopFeedback : MonoBehaviour, IFeedback
    {
        [SerializeField] private float duration = 0.06f;
        [SerializeField, Range(0f, 1f)] private float timeScale = 0f;

        public void PlayFeedback(FeedbackContext context)
            => EventBus<HitStopEvent>.Raise(new HitStopEvent(duration, timeScale)); 
    }
}