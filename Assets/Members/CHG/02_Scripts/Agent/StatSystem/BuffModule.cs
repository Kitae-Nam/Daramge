using System;
using System.Collections.Generic;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    public class BuffModule : MonoBehaviour, IModule, IBuffModule
    {
        private readonly Dictionary<BuffSO, float> _endTimeByBuff = new();
        private readonly List<BuffSO> _expiredBuffs = new();
        
        private IStatModule _statModule;
        private AbstractHealthModule _healthModule;
        
        public void Initialize(ModuleOwner owner)
        {
            _statModule = owner.GetModule<IStatModule>();
            _healthModule = owner.GetModule<AbstractHealthModule>();
            Debug.Assert(_statModule != null, $"statModule is null : {gameObject.name}");
            Debug.Assert(_healthModule != null, $"healthModule is null : {gameObject.name}");

            _healthModule.OnDeath += HandleDeath;
        }

        private void OnDestroy() => _healthModule.OnDeath -= HandleDeath;

        public void ApplyBuff(BuffSO buff)
        {
            if (!_endTimeByBuff.ContainsKey(buff))
                _statModule.AddBonuses(buff, buff.Bonuses);

            _endTimeByBuff[buff] = Time.time + buff.Duration;
        }

        private void Update()
        {
            foreach (KeyValuePair<BuffSO, float> buffPair in _endTimeByBuff)
            {
                if (Time.time >= buffPair.Value)
                    _expiredBuffs.Add(buffPair.Key);
            }

            foreach (BuffSO buff in _expiredBuffs)
            {
                _statModule.RemoveBonuses(buff, buff.Bonuses);
                _endTimeByBuff.Remove(buff);
            }
            
            _expiredBuffs.Clear();
        }

        private void HandleDeath()
        {
            foreach (BuffSO buff in _endTimeByBuff.Keys)
                _statModule.RemoveBonuses(buff, buff.Bonuses);
            
            _endTimeByBuff.Clear();
        }
    }
}