using System;
using System.Collections.Generic;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public abstract class AbstractInteractPoint : MonoBehaviour
    {
        public virtual bool CanInteract => true;
        private readonly HashSet<PlayerInteractor> _interactorsInRange = new();

        private void OnDisable()
        {
            foreach (PlayerInteractor interactor in _interactorsInRange)
                if (interactor != null) interactor.Unregister(this);
            
            _interactorsInRange.Clear();
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out PlayerController player)) return;
            
            PlayerInteractor interactor = player.GetModule<PlayerInteractor>();
            if (interactor == null) return;
            
            if (_interactorsInRange.Add(interactor))
                interactor.Register(this);
        }

        private void OnTriggerExit(Collider collider)
        {
            if (!collider.TryGetComponent(out PlayerController player)) return;
            
            PlayerInteractor interactor = player.GetModule<PlayerInteractor>();
            if (interactor == null) return;
            
            if (_interactorsInRange.Remove(interactor))
                interactor.Unregister(this);
        }

        public abstract void Interact(PlayerController player);
    }
}