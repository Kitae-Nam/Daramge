using UnityEngine;

namespace CHG._02_Scripts.CombatSystem
{
    [CreateAssetMenu(fileName = "Skill data", menuName = "Agent/Skill data", order = 25)]
    public class SkillDataSO : ScriptableObject
    {
        public int skillIndex;
        public string skillName;
        public float cooldown;
        public bool CanInterrupt;
        public bool canBeInterrupted;
        public float skillRange = 1f;
        
        public float baseDamage = 1f;
        public float damageMultiplier = 1f;
    }
}