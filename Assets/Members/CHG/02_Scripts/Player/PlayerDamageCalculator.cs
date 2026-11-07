using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerDamageCalculator : MonoBehaviour, IModule, IAfterInitModule, IDamageCalculator
    {
        [SerializeField] private StatSO strStat;
        [SerializeField] private StatSO criStat;
        [SerializeField] private StatSO cDmgStat;

        private ModuleOwner _owner;
        private IStatModule _statModule;
        private Stat _str;
        private Stat _cri;
        private Stat _cDmg;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _statModule = owner.GetModule<IStatModule>();
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
            Debug.Assert(strStat != null, $"strStat is null : {gameObject.name}");
            Debug.Assert(criStat != null, $"criStat is null : {gameObject.name}");
            Debug.Assert(cDmgStat != null, $"cDmgStat is null : {gameObject.name}");
        }

        public void AfterInit()
        {
            _str = _statModule.GetStat(strStat);
            _cri = _statModule.GetStat(criStat);
            _cDmg = _statModule.GetStat(cDmgStat);
            Debug.Assert(_str != null, $"플레이어에 힘 스탯이 없습니다 : {gameObject.name}");
            Debug.Assert(_cri != null, $"플레이어에 크리티컬 스탯이 없습니다 : {gameObject.name}");
            Debug.Assert(_cDmg != null, $"플레이어에 크리티컬 증뎀 스탯이 없습니다 : {gameObject.name}");
        }

        public DamageData CalculateDamage(SkillDataSO skillData)
        {
            float damage = (_str.Value + skillData.baseDamage) * skillData.damageMultiplier;

            bool isCritical = Random.value < _cri.Value;
            if (isCritical)
            {
                damage *= _cDmg.Value;
            }

            return new DamageData(damage, Vector3.zero, Vector3.zero, _owner, isCritical);
        }
    }
}