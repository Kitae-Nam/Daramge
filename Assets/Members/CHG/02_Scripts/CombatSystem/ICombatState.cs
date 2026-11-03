namespace CHG._02_Scripts.CombatSystem
{
    public interface ICombatState
    {
        bool IsCombat { get; }
        void SetCombat(bool isCombat);
    }
}