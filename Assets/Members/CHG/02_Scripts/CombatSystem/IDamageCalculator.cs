namespace CHG._02_Scripts.CombatSystem
{
    public interface IDamageCalculator
    {
        DamageData CalculateDamage(SkillDataSO skillData);
    }
}