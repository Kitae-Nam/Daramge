using System;
using System.Collections.Generic;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public abstract class AbstractInteractPoint : MonoBehaviour
    {
        private readonly HashSet<PlayerController> _playerInRange = new();

        private void OnDestroy()
        {
            foreach (PlayerController player in _playerInRange)
                player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out PlayerController player)) return;
            if (_playerInRange.Add(player))
                player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
        }

        private void OnTriggerExit(Collider collider)
        {
            if (!collider.TryGetComponent(out PlayerController player)) return;
            if (_playerInRange.Remove(player))
                player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
        }

        protected abstract void HandleInteractKeyPressed();
    }
}