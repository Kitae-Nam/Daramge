using GGMLib.ModuleSystems;
using UnityEngine;

namespace Members.CHG._02_Scripts.Agent
{
    [RequireComponent(typeof(Animator))]
    public class AgentRenderer : MonoBehaviour, IModule, IRenderer
    {
        public Animator Animator { get; private set; }
        protected ModuleOwner _owner;
        public virtual void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            Animator = GetComponent<Animator>();
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            Animator.CrossFadeInFixedTime(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }
    }
}