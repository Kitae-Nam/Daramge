using System;

namespace CHG._02_Scripts.CombatSystem.DotSystem
{
    public interface IDotModule
    {
        event Action<DotSO> OnDotStarted;
        event Action<DotSO> OnDotEnded;
        
        void ApplyDot(DotSO dot);
    }
}