using System;
using Members.CHG._02_Scripts.Agent;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace NKT.Enemy.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimationEnd", story: "[Enemy] wait fo animation end", category: "Action/Animation", id: "ba423ebbe52ef17ef1ecbbdb01bcd52f")]
    public partial class WaitForAnimationEndAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        private AgentTrigger _trigger;
        private bool _isAnimationEnd;
        private IRenderer _agentRenderer;
        
        protected override Status OnStart()
        {
            if(Enemy.Value == null || Enemy.Value.AgentTrigger == null || Enemy.Value.Renderer == null)
                return Status.Failure;

            _isAnimationEnd = false;
            _trigger = Enemy.Value.AgentTrigger;
            _trigger.OnAnimationEnd += HandleAnimationEnd;
            _agentRenderer = Enemy.Value.Renderer;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return _isAnimationEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if(_trigger != null)
                _trigger.OnAnimationEnd -= HandleAnimationEnd;
        }

        private void HandleAnimationEnd()
        {
            if (_agentRenderer.Animator.IsInTransition(0)) return;
            _isAnimationEnd = true;   
        }
    }
}

