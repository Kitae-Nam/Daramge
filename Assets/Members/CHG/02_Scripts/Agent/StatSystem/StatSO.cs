using System;
using System.Collections.Generic;
using GGMLib.DatabaseSystem.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    [CreateAssetMenu(fileName = "Stat data", menuName = "Agent/Stat data", order = 15)]
    public class StatSO : IndexedAsset
    {
        [field: SerializeField] public string StatName { get; private set; }
        [field: SerializeField] public Sprite StatIcon { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public bool IsPercent { get; private set; }

        [SerializeField] private float baseValue;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;
        [SerializeField, TextArea] private string description;
        
        public float MaxValue => maxValue;
        public float MinValue => minValue;
        public float BaseValue => baseValue;

    }
}