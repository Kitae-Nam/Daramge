using CHG._02_Scripts.CoreSystem.DataSystem;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public class PlayParticleVfx : MonoBehaviour, IPlayableVfx
    {
        [field: SerializeField] public AssetNameSO VfxName { get; private set; }
        [field: SerializeField] public float VfxDuration { get; private set; }
        [SerializeField] private ParticleSystem[] particles;
        
        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            PlayVfx();
        }

        public void PlayVfx()
        {
            foreach(ParticleSystem particle in particles)
                particle.Play();
        }

        public void StopVfx()
        {
            foreach(ParticleSystem particle in particles)
                particle.Stop();
        }
    }
}