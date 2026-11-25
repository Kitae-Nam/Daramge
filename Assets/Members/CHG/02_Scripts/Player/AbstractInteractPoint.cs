using System;
using System.Collections.Generic;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public abstract class AbstractInteractPoint : MonoBehaviour
    {
        private readonly HashSet<PlayerInteractor> _interactorsInRange = new();

        private void OnDisable() => ClearInteractors();

        

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

        protected void ClearInteractors()
        {
            foreach (PlayerInteractor interactor in _interactorsInRange)
                if (interactor != null) interactor.Unregister(this);
            
            _interactorsInRange.Clear();
        }
        
        public abstract void Interact(PlayerController player);
        public virtual bool CanInteract(PlayerController player) => true;
    }
}