using Members.CHG._02_Scripts.CombatSystem;
using UnityEngine;

namespace NKT.Enemy
{
    [CreateAssetMenu(fileName = "EnemySkillData", menuName = "Enemy/Enemy skill data", order = 0)]
    public class EnemySkillDataSO : SkillDataSO
    {
        [Header("사용 조건")] 
        public float minRange = 0f;
        public float maxRange = 2f;
        [Min(0)] public float weight = 1f;
        public bool needLineOfSight;

        [Header("연출")] 
        public bool armored;//맞으면 풀리는가
    }
}