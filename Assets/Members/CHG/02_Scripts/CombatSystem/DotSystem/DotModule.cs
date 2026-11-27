using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.DotSystem
{
    public class DotModule : MonoBehaviour, IModule, IDotModule
    {
        private class DotInstance
        {
            public DotSO Data;
            public int Stack;
            public float EndTime;
            public float NextTickTime;
        }
        
        public event Action<DotSO> OnDotStarted;
        public event Action<DotSO> OnDotEnded;

        private readonly List<DotInstance> _dots = new();

        private ITickDamageable _target;
        private AbstractHealthModule _healthModule;
        
        public void Initialize(ModuleOwner owner)
        {
            _target = owner as ITickDamageable;
            _healthModule = owner.GetModule<AbstractHealthModule>();
            Debug.Assert(_target != null, $"owner is not ITickDamageable : {gameObject.name}");
            Debug.Assert(_healthModule != null, $"healthModule is null : {gameObject.name}");

            _healthModule.OnDeath += HandleDeath;
        }

        private void Update()
        {
            for (int i = _dots.Count - 1; i >= 0; i--)
            {
                DotInstance instance = _dots[i];

                if (Time.time >= instance.NextTickTime)
                {
                    instance.NextTickTime += instance.Data.TickInterval;
                    _target.ApplyTickDamage(instance.Data.TickDamage * instance.Stack, instance.Data.TextStyle);
                    if (_dots.Count == 0) return;
                }
                
                if (Time.time >= instance.EndTime)
                {
                    _dots.RemoveAt(i);
                    OnDotEnded?.Invoke(instance.Data);
                }
            }
        }

        private void OnDestroy() => _healthModule.OnDeath -= HandleDeath;

        public void ApplyDot(DotSO dot)
        {
            DotInstance instance = _dots.Find(d => d.Data == dot);
            if (instance == null)
            {
                instance = new DotInstance {Data = dot, NextTickTime = Time.time + dot.TickInterval};
                _dots.Add(instance);
                OnDotStarted?.Invoke(dot);
            }
            
            instance.Stack = Mathf.Min(instance.Stack + 1, dot.MaxStack);
            instance.EndTime = Time.time + dot.Duration;
        }

        private void HandleDeath()
        {
            foreach (DotInstance instance in _dots)
                OnDotEnded?.Invoke(instance.Data);

            _dots.Clear();
        }
    }
}