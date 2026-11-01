using UnityEngine;

namespace CHG._02_Scripts.Player.FSM
{
    public class PlayerSkillState : AbstractPlayerState
    {
        private readonly PlayerSkillModule _skillModule;
        private bool _isSkillEnd;
        
        public PlayerSkillState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _skillModule = agent.GetModule<PlayerSkillModule>();
            Debug.Assert(_skillModule != null, "플레이어 스킬 상태는 스킬 모듈을 필요로 합니다.");
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            _skillModule.OnCurrentSkillEnd += HandleSkillEnd;
            _player.PlayerInput.OnMovementChange += HandleMovementChange;
            _controlMovement.SetMovementDirection(_player.PlayerInput.Movement);
            _isSkillEnd = false;
        }

        public override void Update()
        {
            base.Update();
            if (_isSkillEnd)
            {
                PlayerState nextState = _player.PlayerInput.Movement.magnitude > INPUT_DEADZONE
                    ? PlayerState.RUN : PlayerState.IDLE;
                _player.ChangeState(nextState, 0.1f);
            }
        }

        public override void Exit()
        {
            _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
            _player.PlayerInput.OnMovementChange -= HandleMovementChange;
            base.Exit();
        }

        private void HandleMovementChange(Vector2 movementInput)
            => _controlMovement.SetMovementDirection(movementInput);

        private void HandleSkillEnd() => _isSkillEnd = true;

    }
}