using System;
using System.Collections;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public class PoolableVfx : AbstractMonoPoolable
    {
        [SerializeField] private GameObject effectObject; //우리가 기존에 만든 것
        private IPlayableVfx _playableVfx;
        public IPlayableVfx PlayableVfx => _playableVfx;
        public event Action<PoolableVfx> OnVfxEnd;

        private void Awake()
        {
            _playableVfx = effectObject.GetComponent<IPlayableVfx>();
        }

        private void OnValidate()
        {
            if (effectObject == null) return;
            if (!effectObject.TryGetComponent(out IPlayableVfx _))
            {
                effectObject = null; //드래그한 오브젝트에 IPlayableVfx가 없다면.
            }
        }

        public override void ResetItem()
        {
            _playableVfx.StopVfx(); //풀에 들어갈땐 기존에 재생중이였던 것 정지시키고 
        }

        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            StartCoroutine(PlayVfxCoroutine());
        }

        private IEnumerator PlayVfxCoroutine()
        {
            _playableVfx.PlayVfx();
            yield return new WaitForSeconds(_playableVfx.VfxDuration);
            OnVfxEnd?.Invoke(this);
        }
        
        public void PlayVfx() => StartCoroutine(PlayVfxCoroutine());
    }
}