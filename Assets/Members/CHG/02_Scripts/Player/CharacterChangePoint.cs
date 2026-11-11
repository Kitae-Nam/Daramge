using CHG._02_Scripts.CoreSystem.EventBusSystem;

namespace CHG._02_Scripts.Player
{
    public class CharacterChangePoint : AbstractInteractPoint
    {
        protected override void HandleInteractKeyPressed()
        {
            EventBus<CharacterSelectedRequestEvent>.Raise(new CharacterSelectedRequestEvent());
        }
    }
}