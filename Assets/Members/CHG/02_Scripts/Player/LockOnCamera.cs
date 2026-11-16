using System;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using Unity.Cinemachine;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class LockOnCamera : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera lockOnCamera;
        [SerializeField] private CinemachineTargetGroup targetGroup;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 0;

        private void Awake()
        {
            Debug.Assert(lockOnCamera != null, $"lockOnCamera is null : {gameObject.name}");
            Debug.Assert(targetGroup != null, $"targetGroup is null : {gameObject.name}");
            lockOnCamera.Priority = inactivePriority;
            EventBus<LockOnTargetChangedEvent>.Subscribe(HandleLockOnTargetChanged);
        }

        private void OnDestroy()
        {
            EventBus<LockOnTargetChangedEvent>.Unsubscribe(HandleLockOnTargetChanged);
        }

        private void HandleLockOnTargetChanged(LockOnTargetChangedEvent evt)
        {
            if (evt.Target == null)
            {
                lockOnCamera.Priority = inactivePriority;
                return;
            }
            
            targetGroup.Targets.Clear();
            targetGroup.AddMember(evt.Player.transform, 1f, 0f);
            targetGroup.AddMember(evt.Target, 1f, 0f);
            lockOnCamera.Priority = activePriority;
        }
    }
}