using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class StrafeAnimationModule : MonoBehaviour, IModule
    {
        [SerializeField] private AnimParamSO strafeXParam;
        [SerializeField] private AnimParamSO strafeZParam;
        [SerializeField] private float dampTime = 0.1f;

        private IRenderer _renderer;
        
        public void Initialize(ModuleOwner owner)
        {
            _renderer = owner.GetModule<IRenderer>();
            Debug.Assert(_renderer != null, $"renderer is null : {gameObject.name}");
            Debug.Assert(strafeXParam != null, $"strafeXParam is null : {gameObject.name}");
            Debug.Assert(strafeZParam != null, $"strafeZParam is null : {gameObject.name}");
        }

        public void SetDirection(Vector3 localDirection)
        {
            _renderer.Animator.SetFloat(strafeXParam.ParamHash, localDirection.x, dampTime, Time.deltaTime);
            _renderer.Animator.SetFloat(strafeZParam.ParamHash, localDirection.z, dampTime, Time.deltaTime);
        }
    }
}