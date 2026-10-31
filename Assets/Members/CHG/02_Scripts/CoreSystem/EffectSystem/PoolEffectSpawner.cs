using System;
using CHG._02_Scripts.CoreSystem.Events;
using GGMLib.EventSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public class PoolEffectSpawner : MonoBehaviour
    {
        [SerializeField] private GameEventChannelSO createChannel;
        [SerializeField] private PoolManagerSO poolManager;

        private void Awake()
        {
            Debug.Assert(createChannel != null, $"createChannel is null : {gameObject.name}");
            Debug.Assert(poolManager != null, $"poolManager is null : {gameObject.name}");
            createChannel.AddListener<ShowPoolingEffect>(HandleShowPoolingEffect);
        }

        private void OnDestroy()
        {
            if (createChannel != null)
                createChannel.RemoveListener<ShowPoolingEffect>(HandleShowPoolingEffect);
                
        }

        private void HandleShowPoolingEffect(ShowPoolingEffect evt)
        {
            PoolableVfx vfx = poolManager.Pop<PoolableVfx>(evt.ItemData);
            if (vfx == null)
            {
                Debug.LogWarning($"pool item is not registered : {evt.ItemData}");
                return;
                
            }
            
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(evt.Position, evt.Rotation);
        }

        private void HandleVfxEnd(PoolableVfx vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            poolManager.Push(vfx);
        }
    }
}