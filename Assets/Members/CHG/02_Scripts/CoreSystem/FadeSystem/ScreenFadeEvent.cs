using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.CoreSystem.FadeSystem
{
    public readonly struct ScreenFadeEvent : IEvent
    {
        public float Alpha { get; }
        public float Duration { get; }

        public ScreenFadeEvent(float alpha, float duration)
        {
            Alpha = alpha;
            Duration = duration;
        }
    }
}