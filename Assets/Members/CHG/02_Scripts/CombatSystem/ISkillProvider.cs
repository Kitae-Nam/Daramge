namespace CHG._02_Scripts.CombatSystem
{
    public interface ISkillProvider
    {
        bool TryGetSkill(int skillIndex, out ISkill skill);    
    }
}