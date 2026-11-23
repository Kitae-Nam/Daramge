using System;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.DotSystem
{
    public interface IHitDotModule
    {
        event Action<Collider> OnDotsApplied;
        
        void AddDot(DotSO dot);
        void RemoveDot(DotSO dot);
        void ApplyDots(Collider target);
    }
}