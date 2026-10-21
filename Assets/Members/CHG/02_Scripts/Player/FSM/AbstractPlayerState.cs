using Members.CHG._02_Scripts.FSM;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player.FSM
{
    public abstract class AbstractPlayerState : AgentState
    {
        protected readonly PlayerController _player;
        protected readonly IControlMovement _controlMovement;
        protected const float INPUT_DEADZONE = 0.1f;
        
        protected AbstractPlayerState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _player = agent as PlayerController;
            _controlMovement = agent.GetModule<IControlMovement>();
        }
        
        
    }
}