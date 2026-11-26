using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Respawn
{
    [RequireComponent(typeof(Collider))]
    public class FallZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            
            IRespawnable respawnable = player.GetModule<IRespawnable>();
            respawnable?.RespawnFromFall();
        }
    }
}