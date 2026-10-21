using UnityEngine;

namespace Members.CHG._02_Scripts.CombatSystem
{
    public enum SkillDamageType
    {
        Physical,
        Magical,
    }
    
    [CreateAssetMenu(fileName = "Skill data", menuName = "Agent/Skill data", order = 25)]
    public class SkillDataSO : ScriptableObject
    {
        public int skillIndex;
        public string skillName;
        public float cooldown;
        public float skillRange = 1f;
        
        public SkillDamageType damageType;
        public float baseDamage = 1f;
        public float damageMultiplier = 1f;
    }
}