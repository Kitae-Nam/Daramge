using CHG._02_Scripts.Agent.FSM;
using UnityEngine;

namespace CHG._02_Scripts.Player.FSM
{
    public abstract class AbstractPlayerState : AgentState
    {
        protected const float INPUT_DEADZONE = 0.1f;
        
        protected readonly PlayerController player;
        protected readonly IControlMovement controlMovement;
        protected readonly ILockOnModule lockOn;
        
        protected AbstractPlayerState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            player = agent as PlayerController;
            controlMovement = agent.GetModule<IControlMovement>();
            lockOn = agent.GetModule<ILockOnModule>();
        }
    }
}