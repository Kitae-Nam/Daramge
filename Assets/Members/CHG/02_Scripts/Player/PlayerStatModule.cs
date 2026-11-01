using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CHG._02_Scripts.Player
{
    public class PlayerStatModule : AbstractAgentStatModule
    {
        [Header("Damage related stat list")] 
        [SerializeField] private StatSO strStat;
        [SerializeField] private StatSO criStat;
        [SerializeField] private StatSO cDmgStat;
        [SerializeField] private StatSO attackSpeedStat;
        [SerializeField] private StatSO moveSpeedStat;
        [SerializeField] private StatSO staminaStat;
        

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
            if(!TryGetStat(strStat.AssetIndex, out strStat))
                Debug.LogError("플레이어에 힘 스탯이 없습니다.");
            if(!TryGetStat(criStat.AssetIndex, out criStat))
                Debug.LogError("플레이어에 크리티컬 스탯이 없습니다.");
            if(!TryGetStat(cDmgStat.AssetIndex, out cDmgStat))
                Debug.LogError("플레이어에 크리티컬 증뎀 스탯이 없습니다.");
        }

        public override DamageData CalculateDamage(SkillDataSO skillData)
        {
            float damage = (strStat.Value + skillData.baseDamage) * skillData.damageMultiplier;

            bool isCritical = Random.value < criStat.Value;
            if (isCritical)
            {
                damage *= cDmgStat.Value; 
            }
            
            return new DamageData(damage, Vector3.zero, Vector3.zero, _owner, isCritical);
        }
    }
}