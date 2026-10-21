using System;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace Members.CHG._02_Scripts.Agent
{
    public class AgentTrigger : MonoBehaviour, IModule
    {
        public event Action OnAnimationEnd;
        public event Action OnDamageCast;
        
        public void Initialize(ModuleOwner owner) { }
        
        public void AnimationEndTrigger() => OnAnimationEnd?.Invoke();
        public void DamageCastTrigger() => OnDamageCast?.Invoke();
    }
}