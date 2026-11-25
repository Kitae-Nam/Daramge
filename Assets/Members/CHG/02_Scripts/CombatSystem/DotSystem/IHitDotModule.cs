using System;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.DotSystem
{
    public interface IHitDotModule
    {
        void AddDot(DotSO dot);
        void RemoveDot(DotSO dot);
        void ApplyDots(Collider target);
    }
}