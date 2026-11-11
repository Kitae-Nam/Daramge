using CHG._02_Scripts.CoreSystem.DataSystem;
using UnityEngine;
using UnityEngine.VFX;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public class PlayGraphVfx : MonoBehaviour, IPlayableVfx
    {
        [field: SerializeField] public AssetNameSO VfxName { get; private set; }
        [field: SerializeField] public float VfxDuration { get; private set; }
        [SerializeField] private VisualEffect[] effects;
        
        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            PlayVfx();
        }

        public void PlayVfx()
        {
            foreach(VisualEffect effect in effects)
                effect.Play();
        }

        public void StopVfx()
        {
            foreach(VisualEffect effect in effects)
                effect.Stop();
        }
    }
}