using CHG._02_Scripts.CoreSystem.DataSystem;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public interface IPlayableVfx
    {
        AssetNameSO VfxName { get; }
        float VfxDuration { get; }
        void PlayVfx(Vector3 position, Quaternion rotation);
        void PlayVfx();
        void StopVfx();
    }
}