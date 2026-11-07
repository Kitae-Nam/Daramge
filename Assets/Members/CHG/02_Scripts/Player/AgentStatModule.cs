using System.Collections.Generic;
using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CHG._02_Scripts.Player
{
    public class AgentStatModule : MonoBehaviour, IModule, IStatModule
    {
        [SerializeField] private StatOverride[] statOverrides;

        private Dictionary<StatSO, Stat> _statDict;
        

        public void Initialize(ModuleOwner owner)
        {
            _statDict = new Dictionary<StatSO, Stat>();

            foreach (StatOverride statOverride in statOverrides)
            {
                if (statOverride.StatData == null)
                {
                    Debug.LogWarning($"비어 있는 스탯 항목이 있습니다 : {gameObject.name}");
                    continue;
                }

                if (!_statDict.TryAdd(statOverride.StatData, statOverride.CreateStat()))
                    Debug.LogWarning($"중복된 스탯입니다 : {statOverride.StatData.AssetName}, {gameObject.name}");
            }
        }

        public Stat GetStat(StatSO statData) => _statDict.GetValueOrDefault(statData);

        public void AddBaseValue(StatSO statData, float amount)
        {
            if (_statDict.TryGetValue(statData, out Stat stat))
            {
                stat.BaseValue += amount;
                return;
            }
            
            Debug.LogWarning($"존재하지 않는 스탯입니다 : {statData.AssetName}");
        }

        public void AddModifier(StatSO statData, object key, float modifier,
            StatModifierType type = StatModifierType.Flat)
        {
            if (_statDict.TryGetValue(statData, out Stat stat))
            {
                stat.AddModifier(key, modifier, type);
                return;
            }
            
            Debug.LogWarning($"존재하지 않는 스탯입니다 : {statData.AssetName}");
        }

        public void RemoveModifier(StatSO statData, object key)
        {
            if (_statDict.TryGetValue(statData, out Stat stat))
            {
                stat.RemoveModifier(key);
                return;
            }
            Debug.LogWarning($"존재하지 않는 스탯입니다 : {statData.AssetName}");
        }

        public void AddBonuses(object key, IReadOnlyList<StatBonus> bonuses)
        {
            for (int i = 0; i < bonuses.Count; i++)
                AddModifier(bonuses[i].StatData, (key, i), bonuses[i].Value, bonuses[i].Type);
        }

        public void RemoveBonuses(object key, IReadOnlyList<StatBonus> bonuses)
        {
            for (int i = 0; i < bonuses.Count; i++)
                RemoveModifier(bonuses[i].StatData, (key, i));
        }

        public float SubscribeStat(StatSO statData, Stat.ValueChangeHandler handler, float defaultValue)
        {
            if (_statDict.TryGetValue(statData, out Stat stat))
            {
                stat.OnValueChanged += handler;
                return stat.Value;
            }
            
            Debug.LogWarning($"존재하지 않는 스탯입니다 : {statData.AssetName}");
            return defaultValue;
        }

        public void UnSubscribeStat(StatSO statData, Stat.ValueChangeHandler handler)
        {
            if (_statDict.TryGetValue(statData, out Stat stat))
            {
                stat.OnValueChanged -= handler;
            }
        }
    }
}