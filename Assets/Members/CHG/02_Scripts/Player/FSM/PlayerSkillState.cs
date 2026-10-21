using UnityEngine;

namespace Members.CHG._02_Scripts.Player.FSM
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
            _isSkillEnd = false;
        }

        public override void Update()
        {
            base.Update();
            if(_isSkillEnd)
                _player.ChangeState(PlayerState.IDLE, 0.1f);
        }

        public override void Exit()
        {
            _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
            base.Exit();
        }

        private void HandleSkillEnd() => _isSkillEnd = true;

    }
}