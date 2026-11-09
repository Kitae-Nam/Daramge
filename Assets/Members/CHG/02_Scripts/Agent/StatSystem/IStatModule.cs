using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;

namespace CHG._02_Scripts.Agent.StatSystem
{
    public interface IStatModule
    {
        Stat GetStat(StatSO statData);
        void AddBaseValue(StatSO statData, float amount);
        void AddModifier(StatSO statData, object key, float modifier, StatModifierType type = StatModifierType.Flat);
        void RemoveModifier(StatSO statData, object key);
        void AddBonuses(object key, IReadOnlyList<StatBonus> bonuses);
        void RemoveBonuses(object key, IReadOnlyList<StatBonus> bonuses);
        float SubscribeStat(StatSO statData, Stat.ValueChangeHandler handler, float defaultValue);
        void UnSubscribeStat(StatSO statData, Stat.ValueChangeHandler handler);
    }
}