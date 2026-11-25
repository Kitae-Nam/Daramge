using System;
using UnityEngine;
using UnityEngine.Events;

namespace CHG._02_Scripts.Player.Respawn
{
    [RequireComponent(typeof(Collider))]
    public class RespawnArea : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private UnityEvent onPlayerRespawned;
        
        public Vector3 RespawnPosition => respawnPoint.position;

        private void Awake()
        {
            Debug.Assert(respawnPoint != null, $"respawn point is null : {gameObject.name}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryGetRespawnable(other, out IRespawnable respawnable))
                respawnable.EnterArea(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (TryGetRespawnable(other, out IRespawnable respawnable))
                respawnable.ExitArea(this);
        }
        
        public void NotifyRespawned() 
            => onPlayerRespawned?.Invoke();

        private static bool TryGetRespawnable(Collider other, out IRespawnable respawnable)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            respawnable = player != null ? player.GetModule<IRespawnable>() : null;
            return respawnable != null;
        }
    }
}