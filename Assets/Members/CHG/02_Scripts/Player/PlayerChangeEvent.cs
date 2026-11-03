using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.Player
{
    public readonly struct CharacterChangeStartEvent : IEvent
    {
        public PlayerController Prev { get;  }
        public PlayerController Current { get;  }

        public CharacterChangeStartEvent(PlayerController prev, PlayerController current)
        {
            Prev = prev;
            Current = current;
        }
    }

    public readonly struct CharacterChangeEndedEvent : IEvent
    {
        public PlayerController Prev { get; }
        public PlayerController Current { get; }

        public CharacterChangeEndedEvent(PlayerController prev, PlayerController current)
        {
            Prev = prev;
            Current = current;
        }
    }
    
    public readonly struct CharacterSelectedRequestEvent : IEvent {}
}