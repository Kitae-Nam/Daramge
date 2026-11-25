using UnityEngine;

namespace CHG._02_Scripts.Player.FSM
{
    public class PlayerInteractState : AbstractPlayerState
    {
        private readonly ICarrier _carrier;
        
        public PlayerInteractState(Agent.Agent agent, int stateClipHash) : base(agent, stateClipHash)
            =>  _carrier = agent.GetModule<ICarrier>();

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            controlMovement.SetMovementDirection(Vector2.zero);
        }

        public override void Update()
        {
            base.Update();
            if (_carrier == null || !_carrier.IsActing)
                player.ChangeState(GetDefaultState(), 0.1f);
        }
    }
}