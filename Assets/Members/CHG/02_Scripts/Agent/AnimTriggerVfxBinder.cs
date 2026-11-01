using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.DataSystem;
using CHG._02_Scripts.CoreSystem.EffectSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Agent
{
    [Serializable]
    public struct TriggerVfxPair
    {
        public AnimTriggerSO trigger;
        public AnimTriggerSO stopTrigger;
        public AssetNameSO vfxName;
    }
    
    public class AnimTriggerVfxBinder : MonoBehaviour, IModule
    {
        
        [SerializeField] private TriggerVfxPair[] pairs;

        private AgentTrigger _trigger;
        private VfxModule _vfxModule;
        private readonly List<(AnimTriggerSO trigger, Action handler)> _handlers = new();
        
        public void Initialize(ModuleOwner owner)
        {
            _trigger = owner.GetModule<AgentTrigger>();
            _vfxModule = owner.GetModule<VfxModule>();
            Debug.Assert(_trigger != null, $"AgentTrigger is null : {gameObject.name}");
            Debug.Assert(_vfxModule != null, $"VfxModule is null : {gameObject.name}");

            foreach (TriggerVfxPair pair in pairs)
            {
                Debug.Assert(pair.trigger != null && pair.vfxName != null, $"Trigger is null : {gameObject.name}");
                if (pair.trigger == null || pair.vfxName == null) continue;

                int vfxHash = pair.vfxName.AssetHash;
                Bind(pair.trigger, () => _vfxModule.PlayVfx(vfxHash));
                if (pair.stopTrigger != null)
                    Bind(pair.stopTrigger, () => _vfxModule.StopVfx(vfxHash));
            }
        }

        private void Bind(AnimTriggerSO trigger, Action handler)
        {
            _trigger.Subscribe(trigger, handler);
            _handlers.Add((trigger, handler));
        }

        private void OnDestroy()
        {
            if (_trigger == null) return;
            foreach ((AnimTriggerSO trigger, Action handler) in _handlers)
                _trigger.UnSubscribe(trigger, handler);
        }
    }
}