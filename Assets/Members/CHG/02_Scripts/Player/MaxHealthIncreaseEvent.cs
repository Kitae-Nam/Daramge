using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.Player
{
    public readonly struct MaxHealthIncreaseEvent : IEvent
    {
        public int Amount { get; }
        public MaxHealthIncreaseEvent(int amount) => Amount = amount;
    }
}