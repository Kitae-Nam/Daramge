using System.Linq;
using CHG._02_Scripts.CoreSystem.DataSystem;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace CHG._02_Scripts.CoreSystem.EffectSystem
{
    public class DisintegrateVfx : MonoBehaviour, IPlayableVfx
    {
        [field: SerializeField] public AssetNameSO VfxName { get; private set; }
        [field: SerializeField] public float VfxDuration { get; private set; }

        [SerializeField] private VisualEffect effect;
        
        private static readonly int MeshFormId = Shader.PropertyToID("MeshForm");

        private VFXPropertyBinder _binder;
        private MyTransformBinder _myBinder;
        private SkinnedMeshRenderer _skinnedMesh;

        private void Awake()
        {
            _binder = GetComponent<VFXPropertyBinder>();
            _myBinder = _binder.GetPropertyBinders<MyTransformBinder>().First();
            _myBinder.Property = "Transform";
        }

        public void SetEffectData(Transform target, SkinnedMeshRenderer skinnedMesh)
        {
            _myBinder.Target = target;
            _skinnedMesh = skinnedMesh;
        }
        
        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            PlayVfx();
        }

        public void PlayVfx()
        {
            effect.SetSkinnedMeshRenderer(MeshFormId, _skinnedMesh);
            effect.Play();
        }

        public void StopVfx()
        {
            effect.Stop();
        }
    }
}