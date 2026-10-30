using System;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Agent
{
    [Serializable]
    public struct AnimTriggerPoint
    {
        public AnimTriggerSO trigger;
        [Range(0f, 1f)] public float normalizedTime;
    }
    
    public class AnimTriggerBehaviour : StateMachineBehaviour
    {
        [SerializeField] private AnimTriggerPoint[] triggerPoints;
        [SerializeField] private AnimTriggerSO[] interruptTriggers;
        
        private AgentTrigger _trigger;
        private int _nextIndex;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_trigger == null)
            {
                _trigger = animator.GetComponent<AgentTrigger>();
                Debug.Assert(_trigger != null, $"AgentTrigger is null : {animator.gameObject.name}");
            }
            _nextIndex = 0;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_trigger == null || IsLeavingState(animator, stateInfo, layerIndex)) return;

            while (_nextIndex < triggerPoints.Length &&
                   stateInfo.normalizedTime >= triggerPoints[_nextIndex].normalizedTime)
            {
                _trigger.Raise(triggerPoints[_nextIndex].trigger);
                _nextIndex++;
            }
        }

        private bool IsLeavingState(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!animator.IsInTransition(layerIndex)) return false;

            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
            return next.fullPathHash != stateInfo.fullPathHash || !Mathf.Approximately(next.normalizedTime, stateInfo.normalizedTime);
        }

        private void OnValidate() => Array.Sort(triggerPoints, (a, b) => a.normalizedTime.CompareTo(b.normalizedTime));

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_trigger == null || _nextIndex >= triggerPoints.Length) return;
            
            foreach (AnimTriggerSO interruptTrigger in interruptTriggers)
                _trigger.Raise(interruptTrigger);
        }
    }
}