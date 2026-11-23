namespace CHG._02_Scripts.Player
{
    public interface ICarrier
    {
        bool TryHandleInteract();
        void RequestPickUp(PushableCrate crate);
    }
}