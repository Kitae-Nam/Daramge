using System;
using System.Collections.Generic;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.DotSystem
{
    public class HitDotModule : MonoBehaviour, IModule, IHitDotModule
    {
        private readonly List<DotSO> _dots = new();

        public void Initialize(ModuleOwner owner) { }

        public void AddDot(DotSO dot) => _dots.Add(dot);

        public void RemoveDot(DotSO dot) => _dots.Remove(dot);

        public void ApplyDots(Collider target)
        {
            if (_dots.Count == 0) return;
            if (!target.TryGetComponent(out ModuleOwner targetOwner)) return;
            
            IDotModule dotModule = targetOwner.GetModule<IDotModule>();
            if (dotModule == null)
            {
                Debug.LogWarning($"DotModule is null : {target.name}");
                return;
            }
            
            foreach (DotSO dot in _dots)
                dotModule.ApplyDot(dot);
        }
    }
}