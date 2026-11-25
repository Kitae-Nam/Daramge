using CHG._02_Scripts.Player.Hold;

namespace CHG._02_Scripts.Player
{
    public interface ICarrier
    {
        bool IsActing { get; }
        
        bool TryHandleInteract();
        void RequestPickUp(IHoldable holdable);
    }
}