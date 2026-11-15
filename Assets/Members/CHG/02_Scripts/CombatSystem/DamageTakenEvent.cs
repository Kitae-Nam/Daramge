using CHG._02_Scripts.CoreSystem.EventBusSystem;
using GGMLib.ModuleSystems;

namespace CHG._02_Scripts.CombatSystem
{
    public readonly struct DamageTakenEvent : IEvent
    {
        public ModuleOwner Target { get; }
        public float Amount { get; }
        public bool IsCritical { get; }

        public DamageTakenEvent(ModuleOwner target, float amount, bool isCritical)
        {
            Target = target;
            Amount = amount;
            IsCritical = isCritical;
        }
    }
}