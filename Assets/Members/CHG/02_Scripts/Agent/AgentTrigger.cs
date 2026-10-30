using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Agent
{
    public class AgentTrigger : MonoBehaviour, IModule
    {
        public event Action OnAnimationEnd;
        public event Action OnDamageCast;
        
        private readonly Dictionary<AnimTriggerSO, Action> _triggerEvents = new Dictionary<AnimTriggerSO, Action>();
        
        public void Initialize(ModuleOwner owner) { }
        
        public void AnimationEndTrigger() => OnAnimationEnd?.Invoke();
        public void DamageCastTrigger() => OnDamageCast?.Invoke();

        public void Subscribe(AnimTriggerSO trigger, Action handler)
        {
            _triggerEvents.TryGetValue(trigger, out Action handlers);
            _triggerEvents[trigger] = handlers += handler;
        }

        public void UnSubscribe(AnimTriggerSO trigger, Action handler)
        {
            if (trigger == null || !_triggerEvents.TryGetValue(trigger, out Action handlers)) return;
            
            handlers -= handler;
            if (handlers == null)
                _triggerEvents.Remove(trigger);
            else 
                _triggerEvents[trigger] = handlers;
        }

        public void Raise(AnimTriggerSO trigger)
        {
            Debug.Assert(trigger != null, $"anim trigger is null : {gameObject.name}");
            if (trigger != null && _triggerEvents.TryGetValue(trigger, out Action handlers))
                handlers?.Invoke();
        }
    }
}