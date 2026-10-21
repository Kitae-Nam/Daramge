using System;
using UnityEngine;

namespace Members.CHG._02_Scripts.Agent.StatSystem
{
    [Serializable]
    public class StatOverride
    {
        [field: SerializeField] public StatSO StatData { get; private set; }
        [SerializeField] private bool isUseOverride;
        [SerializeField] private float overrideValue;
        
        public StatOverride(StatSO statData) => StatData = statData;

        public StatSO CloneStat()
        {
            Debug.Assert(StatData != null, "복제할 스탯이 없습니다.");
            StatSO clonedStat = StatData.Clone() as StatSO;
            Debug.Assert(clonedStat != null, "복제가 올바르게 되지 않았습니다.");
            if (isUseOverride)
            {
                clonedStat.BaseValue = overrideValue;
            }
            return clonedStat;
        }
    }
}