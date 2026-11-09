using System;
using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    [Serializable]
    public class StatOverride
    {
        [field: SerializeField] public StatSO StatData { get; private set; }
        [SerializeField] private bool isUseOverride;
        [SerializeField] private float overrideValue;

        public Stat CreateStat()
        {
            Debug.Assert(StatData != null, "생성할 스탯 데이터가 없습니다.");
            return new Stat(StatData, isUseOverride ? overrideValue : StatData.BaseValue);
        }
    }
}