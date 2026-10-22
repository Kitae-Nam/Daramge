using Members.CHG._02_Scripts.Agent.FSM;
using Members.CHG._02_Scripts.Player.FSM;
using UnityEngine;

namespace Members.CHG._02_Scripts.Player
{
    public class PlayerController : Agent.Agent
    {
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO playerStates;
        
        private StateMachine _stateMachine;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _stateMachine = new StateMachine(this, playerStates.states);
        }

        protected override void HandleHitEvent()
        {
            
        }

        private void Start()
        {
            ChangeState(PlayerState.IDLE, transitionDuration: 0); 
        }

        private void Update()
        {
            _stateMachine.UpdateMachine();
        }
        
        public void ChangeState(PlayerState newState, float transitionDuration)
            => _stateMachine.ChangeState((int)newState, transitionDuration);
    }
}