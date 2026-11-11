using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem
{
    public struct HitStopEvent : IEvent
    {
        public float Duration { get; }
        public float TimeScale { get; }

        public HitStopEvent(float duration, float timeScale)
        {
            Duration = duration;
            TimeScale = timeScale;
        }
        
    }
}