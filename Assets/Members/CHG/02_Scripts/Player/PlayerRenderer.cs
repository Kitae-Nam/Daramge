using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerRenderer : AgentRenderer
    {
        [SerializeField] private AnimParamSO moveXParam;
        [SerializeField] private AnimParamSO moveZParam;
        [SerializeField] private float moveDampTime = 0.1f;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Debug.Assert(moveXParam != null, $"strafeXParam is null : {gameObject.name}");
            Debug.Assert(moveZParam != null, $"strafeZParam is null : {gameObject.name}");
        }

        public void SetMoveDirection(Vector3 localDir)
        {
            Animator.SetFloat(moveXParam.ParamHash, localDir.x, moveDampTime, Time.deltaTime);
            Animator.SetFloat(moveZParam.ParamHash, localDir.z, moveDampTime, Time.deltaTime);
        }
    }
}