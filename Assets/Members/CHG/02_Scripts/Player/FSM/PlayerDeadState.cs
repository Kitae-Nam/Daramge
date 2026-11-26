using UnityEngine;

namespace CHG._02_Scripts.Player.FSM
{
    public class PlayerDeadState : AbstractPlayerState
    {
        public PlayerDeadState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            controlMovement.SetMovementDirection(Vector2.zero);
        }
    }
}