using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.CoreSystem.LetterboxSystem
{
    public readonly struct LetterboxRequestEvent : IEvent
    {
        public object Requester { get; }
        public LetterboxSetting Settings { get; }

        public LetterboxRequestEvent(object requester, LetterboxSetting settings)
        {
            Requester = requester;
            Settings = settings;
        }
    }

    public readonly struct LetterboxReleaseEvent : IEvent
    {
        public object Requester { get; }

        public LetterboxReleaseEvent(object requester) => Requester = requester;
    }
}
