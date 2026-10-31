using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.CoreSystem.TimeSystem
{
    public struct TimeScaleRequestEvent : IEvent
    {
        public object Requester { get; }
        public float TimeScale { get; }

        public TimeScaleRequestEvent(object requester, float timeScale)
        {
            Requester = requester;
            TimeScale = timeScale;
        }
        
    }

    public readonly struct TimeScaleReleaseEvent : IEvent
    {
        public object Requester { get; }
        
        public TimeScaleReleaseEvent(object requester)
            => Requester = requester;
    }
}