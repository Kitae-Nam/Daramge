using UnityEngine;

namespace Members.CHG._02_Scripts.Agent
{
    public interface IRenderer
    {
        Animator Animator { get; }
        void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0);
    }
}