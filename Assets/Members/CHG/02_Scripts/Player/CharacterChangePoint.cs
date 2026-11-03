using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class CharacterChangePoint : MonoBehaviour
    {
        private readonly HashSet<PlayerController> _playersInRange = new();

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            Debug.Assert(trigger.isTrigger, $"collider is not trigger : {gameObject.name}");
        }

        private void OnDestroy()
        {
            foreach (PlayerController player in _playersInRange)
                player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
            _playersInRange.Clear();
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out PlayerController player)) return;
            if (_playersInRange.Add(player)) 
                player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
        }

        private void OnTriggerExit(Collider collider)
        {
            if (!collider.TryGetComponent(out PlayerController player)) return;
            if (_playersInRange.Remove(player))
                player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
        }

        private void HandleInteractKeyPressed()
            => EventBus<CharacterSelectedRequestEvent>.Raise(new CharacterSelectedRequestEvent());
    }
}