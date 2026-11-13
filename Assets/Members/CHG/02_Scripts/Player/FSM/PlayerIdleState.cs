using UnityEngine;

namespace CHG._02_Scripts.Player.FSM
{
    public class PlayerIdleState : AbstractPlayerState
    {
        public PlayerIdleState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            player.PlayerInput.OnMovementChange += HandleMovementChange;
            controlMovement.SetMovementDirection(Vector2.zero);
        }

        public override void Update()
        {
            base.Update();
            if (lockOn.HasTarget)
                player.ChangeState(PlayerState.LOCKON, 0.1f);
        }

        public override void Exit()
        {
            player.PlayerInput.OnMovementChange -= HandleMovementChange;
            base.Exit();
        }

        private void HandleMovementChange(Vector2 movementKey)
        {
            if (movementKey.magnitude > INPUT_DEADZONE)
            {
                player.ChangeState(PlayerState.RUN, transitionDuration: 0.1f); 
            }
        }
    }
}