using System;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player.Respawn
{
    public class SafeGroundTracker : MonoBehaviour, IModule
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float edgeCheckRadius = 0.8f;
        [SerializeField] private float checkDistance = 1.5f;
        [SerializeField] private float recordInterval = 0.2f;

        private static readonly Vector3[] _edgeDirections =
            { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        
        private CharacterController _characterController;
        private Transform _ownerTrm;
        private float _nextRecordTime;
        
        public Vector3 LastSafePosition { get; private set; }
        
        public void Initialize(ModuleOwner owner)
        {
            _characterController = owner.GetComponent<CharacterController>();
            _ownerTrm = owner.transform;
            LastSafePosition = _ownerTrm.position;
            Debug.Assert(_characterController != null,$"characterController is null : {gameObject.name}");
        }

        private void FixedUpdate()
        {
            if (Time.time < _nextRecordTime) return;
            _nextRecordTime = Time.time + recordInterval;
            
            if (_characterController.isGrounded && IsSafeGround(_ownerTrm.position))
                LastSafePosition = _ownerTrm.position;
        }

        private bool IsSafeGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * 0.5f;
            foreach (Vector3 direction in _edgeDirections)
            {
                if (!Physics.Raycast(origin + direction * edgeCheckRadius, Vector3.down, checkDistance, groundLayer))
                    return false;
            }

            return true;
        }
    }
}