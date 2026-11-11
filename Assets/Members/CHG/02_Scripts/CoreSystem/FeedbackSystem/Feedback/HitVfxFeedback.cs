using System;
using CHG._02_Scripts.CoreSystem.EffectSystem;
using CHG._02_Scripts.CoreSystem.Events;
using GGMLib.EventSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem.Feedback
{
    public class HitVfxFeedback : MonoBehaviour, IFeedback
    {
        [SerializeField] private GameEventChannelSO createChannel;
        [SerializeField] private PoolItemSO hitVfxItem;

        private void Awake()
        {
            Debug.Assert(createChannel != null, $"createChannel is null : {gameObject.name}");
            Debug.Assert(hitVfxItem != null, $"hitVfxItem is null : {gameObject.name}");
        }

        public void PlayFeedback(FeedbackContext context)
        {
            Quaternion rotation = Quaternion.LookRotation(context.HitDirection);
            
            createChannel.RaiseEvent(CreateEvents.ShowPoolingEffect.InitData(
                hitVfxItem, context.HitPoint, rotation)); 
        }
    }
}