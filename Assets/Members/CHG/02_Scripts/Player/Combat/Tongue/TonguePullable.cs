using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat.Tongue
{
    [RequireComponent(typeof(Rigidbody))]
    public class TonguePullable : MonoBehaviour, ITonguePullable
    {
        private const float PLACE_CHECK_SCALE = 0.9f;
        
        private Rigidbody _rb;
        private Collider[] _colliders;
        private bool _wasKinematic;
        private Vector3 _boundsOffset;
        private Vector3 _halfExtents;

        public Transform Root => transform;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();
            Debug.Assert(_colliders.Length > 0, $"Collider is null : {gameObject.name}");
        }

        public void BeginPull()
        {
            CacheBounds();
            _wasKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
            SetColliderEnabled(false);
        }

        public void EndPull()
        {
            SetColliderEnabled(true);   
            _rb.isKinematic = _wasKinematic;
        }
        
        private void SetColliderEnabled(bool isEnabled)
        {
            foreach (Collider col in _colliders)
                col.enabled = isEnabled;
        }
        
        public bool CanPlace(LayerMask obstacleMask) 
            => !Physics.CheckBox(transform.position + _boundsOffset, _halfExtents * PLACE_CHECK_SCALE,
                Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);

        private void CacheBounds()
        {
            Bounds bounds = _colliders[0].bounds;
            foreach (Collider col in _colliders)
                bounds.Encapsulate(col.bounds);
            _boundsOffset = bounds.center - transform.position;
            _halfExtents = bounds.extents;
        }
    }
}