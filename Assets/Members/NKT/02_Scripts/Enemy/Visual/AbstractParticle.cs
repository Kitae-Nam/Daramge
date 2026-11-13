using System;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace NKT.Enemy.Visual
{
    public abstract class AbstractParticle : MonoBehaviour, IPoolable
    {
        protected ParticleSystem particle;
        
        #region Pool

        [field: SerializeField] public PoolItemSO PoolItem { get;  set; }
        public GameObject GameObject => this != null ?  this.gameObject : null;
        public void ResetItem()
        {
            
        }

        #endregion

        private void Awake()
        {
            particle = GetComponent<ParticleSystem>();
        }

        public void Play()
        {
            particle.Play();
        }
    }
}