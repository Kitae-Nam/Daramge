using System;
using System.Collections.Generic;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerInteractor : MonoBehaviour, IModule
    {
        private readonly HashSet<AbstractInteractPoint> _pointsInRange = new();
        private PlayerController _player;
        private ICarrier _carrier;
        
        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            Debug.Assert(_player != null, $"not player child : {gameObject.name}");
            
            _carrier = owner.GetModule<ICarrier>();
            _player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
        }

        private void OnDestroy()
        {
            if (_player != null && _player.PlayerInput != null)
                _player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
        }
        
        public void Register(AbstractInteractPoint point) => _pointsInRange.Add(point);
        public void Unregister(AbstractInteractPoint point) => _pointsInRange.Remove(point);

        private void HandleInteractKeyPressed()
        {
            if (_carrier != null && _carrier.TryHandleInteract()) return;

            AbstractInteractPoint target = FindClosestPoint();
            if (target != null)
                target.Interact(_player);
        }

        private AbstractInteractPoint FindClosestPoint()
        {
            AbstractInteractPoint closest = null;
            float closestDistance = float.MaxValue;
            Vector3 position = _player.transform.position;

            foreach (AbstractInteractPoint point in _pointsInRange)
            {
                if (point == null || !point.CanInteract) continue;

                float distance = (point.transform.position - position).sqrMagnitude;
                if (distance >= closestDistance) continue;
                
                closestDistance = distance;
                closest = point;
            }
            
            return closest;
        }
    }
}