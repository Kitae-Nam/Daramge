using CHG._02_Scripts.Agent;
using CHG._02_Scripts.Agent.FSM;
using CHG._02_Scripts.Player.FSM;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerController : Agent.Agent
    {
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO playerStates;
        
        private StateMachine _stateMachine;
        
        public PlayerState CurrentState { get; private set; }
        public bool IsFrozen { get; private set; }

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
        {
            CurrentState = newState;
            _stateMachine.ChangeState((int)newState, transitionDuration);
        }
        
        public void Freeze() 
        {
            if (IsFrozen) return;
            IsFrozen = true;
            PlayerInput.DisableInput();
            AddStatus(AgentStatus.Invincible);
        }

        public void Unfreeze() 
        {
            if (!IsFrozen) return;
            IsFrozen = false;
            RemoveStatus(AgentStatus.Invincible);
            PlayerInput.EnableInput();
        }
    }
}