namespace Members.CHG._02_Scripts.Agent
{
    public abstract class AbstractAgentState
    {
        protected Agent _agent;
        protected readonly int _stateClipHash;

        protected IRenderer _renderer;

        public AbstractAgentState(Agent agent, int stateClipHash)
        {
            _agent = agent;
            _stateClipHash = stateClipHash;
            _renderer = _agent.GetModule<IRenderer>();
        }

        public virtual void Enter(float transitionDuration, int layerIndex = 0)
        {
            _renderer.PlayClip(_stateClipHash, 0f, transitionDuration, layerIndex);
        }

        public virtual void Update() { }
        
        public virtual void Exit() {}
    }
}