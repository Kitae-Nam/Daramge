using System.Collections.Generic;
using System.Linq;
using GGMLib.ModuleSystems;
using Members.CHG._02_Scripts.CombatSystem;
using UnityEngine;

namespace Members.CHG._02_Scripts.Agent.StatSystem
{
    public abstract class AbstractAgentStatModule : MonoBehaviour, IModule, IStatModule
    {
        [SerializeField] private StatOverride[] statOverrides;

        protected ModuleOwner _owner;
        protected Dictionary<int, StatSO> _statDict; //진짜 복제된 스탯들이 들어가는곳
        
        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _statDict = statOverrides.ToDictionary(
                s => s.StatData.AssetIndex, s => s.CloneStat());
        }
        
        public StatSO GetStat(int statIndex) => _statDict.GetValueOrDefault(statIndex);
        
        public bool TryGetStat(int statIndex, out StatSO outStat)
            => _statDict.TryGetValue(statIndex, out outStat);

        public void AddModifier(int statIndex, object key, float modifier)
        {
            if (_statDict.TryGetValue(statIndex, out StatSO stat))
            {
                stat.AddModifier(key, modifier);
                return;
            }
            Debug.LogWarning($"존재하지 않는 스탯입니다 : {statIndex}");
        }

        public void RemoveModifier(int statIndex, object key)
        {
            if (_statDict.TryGetValue(statIndex, out StatSO stat))
            {
                stat.RemoveModifier(key);
                return;
            }
            Debug.LogWarning($"존재하지 않는 스탯입니다 : {statIndex}");
        }

        public float SubscribeStat(int statIndex, StatSO.ValueChangeHandler handler, float defaultValue)
        {
            if (_statDict.TryGetValue(statIndex, out StatSO stat))
            {
                stat.OnValueChanged += handler;
                return stat.Value;
            }

            return defaultValue;
        }

        public void UnSubscribeStat(int statIndex, StatSO.ValueChangeHandler handler)
        {
            if (_statDict.TryGetValue(statIndex, out StatSO stat))
            {
                stat.OnValueChanged -= handler;
            }
        }

        public abstract DamageData CalculateDamage(SkillDataSO skillData);
    }
}