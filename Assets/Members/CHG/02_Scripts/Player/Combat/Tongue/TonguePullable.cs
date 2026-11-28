using System;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat.Tongue
{
    [RequireComponent(typeof(Rigidbody))]
    public class TonguePullable : MonoBehaviour, ITonguePullable
    {
        private Rigidbody _rb;
        private Collider[] _colliders;
        private bool _wasKinematic;
        protected Vector3 boundsOffset;
        protected Vector3 halfExtents;

        public Transform Root => transform;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();
            Debug.Assert(_colliders.Length > 0, $"Collider is null : {gameObject.name}");
            CacheBounds();
        }

        public void BeginPull()
        {
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
        
        private void CacheBounds()
        {
            Quaternion rotation = transform.rotation;  
            transform.rotation = Quaternion.identity;  
            Physics.SyncTransforms();                  

            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Collider col in _colliders)
            {
                if (col.isTrigger) continue;
                if (!hasBounds)
                {
                    bounds = col.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(col.bounds);
            }

            boundsOffset = bounds.center - transform.position;
            halfExtents = bounds.extents;

            transform.rotation = rotation;  
        }
    }
}