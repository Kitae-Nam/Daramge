using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.Player
{
    public class CharacterChangePoint : AbstractInteractPoint
    {
        public override void Interact(PlayerController player)
        {
            EventBus<CharacterSelectedRequestEvent>.Raise(new CharacterSelectedRequestEvent());
        }
    }
}