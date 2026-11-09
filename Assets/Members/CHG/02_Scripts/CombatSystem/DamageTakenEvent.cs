using CHG._02_Scripts.CoreSystem.EventBusSystem;
using GGMLib.ModuleSystems;

namespace CHG._02_Scripts.CombatSystem
{
    public readonly struct DamageTakenEvent : IEvent
    {
        public ModuleOwner Target { get; }
        public float Amount { get; }

        public DamageTakenEvent(ModuleOwner target, float amount)
        {
            Target = target;
            Amount = amount;
        }
    }
}