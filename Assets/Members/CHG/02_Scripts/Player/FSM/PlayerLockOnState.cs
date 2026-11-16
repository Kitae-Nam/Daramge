using UnityEngine;

namespace CHG._02_Scripts.Player.FSM
{
    public class PlayerLockOnState : AbstractPlayerState
    {
        private readonly PlayerRenderer _playerRenderer;

        public PlayerLockOnState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _playerRenderer = _renderer as PlayerRenderer;
            Debug.Assert(_playerRenderer != null, "playerRenderer is null : LockOnState");
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            player.PlayerInput.OnMovementChange += HandleMovementChange;
            controlMovement.SetMovementDirection(player.PlayerInput.Movement);
            controlMovement.CanAutoRotate = false;
        }

        public override void Update()
        {
            base.Update();
            if (!lockOn.HasTarget)
            {
                PlayerState nextState = player.PlayerInput.Movement.magnitude > INPUT_DEADZONE
                    ? PlayerState.RUN : PlayerState.IDLE;
                player.ChangeState(nextState, 0.1f);
                return;
            }

            controlMovement.RotateTo(lockOn.DirectionToTarget);
            Vector3 worldDirection = controlMovement.ToWorldDirection(player.PlayerInput.Movement);
            _playerRenderer.SetMoveDirection(player.transform.InverseTransformDirection(worldDirection)); 
        }

        public override void Exit()
        {
            player.PlayerInput.OnMovementChange -= HandleMovementChange;
            controlMovement.CanAutoRotate = true;
            base.Exit();
        }

        private void HandleMovementChange(Vector2 movementInput)
            => controlMovement.SetMovementDirection(movementInput);
    }
}