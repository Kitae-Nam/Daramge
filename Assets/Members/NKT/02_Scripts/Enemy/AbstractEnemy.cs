using Members.CHG._02_Scripts.Agent;
using NKT.Enemy.BT;
using NKT.Enemy.BT.Events;
using NKT.Enemy.Module;
using NKT.Enemy.Skill;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy
{
    [RequireComponent(typeof(BehaviorGraphAgent), typeof(NavMeshAgent))]
    public abstract class AbstractEnemy : Agent
    {
        [field:SerializeField] public EnemyDataSO EnemyData {get; private set;}
        
        public INavMovement NavMovement { get; private set; }
        public IRenderer Renderer { get; private set; }
        public AgentSensor AgentSensor { get; private set; }
        public AgentTrigger AgentTrigger { get; private set; }
        public BehaviorGraphAgent BTAgent { get; private set; }
        public StateChannel  StateChannel { get; private set; }
        public SkillEventRelay SkillEventRelay { get; private set; }
        public EnemySkillModule SkillModule { get; private set; }
        public Vector3 HomePoint => _homePoint;

        private Vector3 _homePoint;
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            NavMovement = GetModule<INavMovement>();
            Renderer = GetModule<IRenderer>();
            AgentSensor = GetModule<AgentSensor>();
            AgentTrigger = GetModule<AgentTrigger>();
            SkillEventRelay = GetModule<SkillEventRelay>();
            SkillModule = GetModule<EnemySkillModule>();
            
            BTAgent = GetComponent<BehaviorGraphAgent>();
        }

        public virtual void Start()
        {
            SetBTVariable(BTVar.Enemy, this);

            if (GetBTVariable(BTVar.StateChannel, out BlackboardVariable<StateChannel> channel))
                StateChannel = channel.Value;

            _homePoint = transform.position;
        }

        protected override void HandleHitEvent()
        {
            if (IsDead) return;
            if (SkillModule != null && SkillModule.IsArmored) return; //데미지는 들어가고 경직만 무시

            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = false;
            if (SkillModule != null)
                SkillModule.CancelCurrentSkill();
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
        #region BT Helper

        protected bool SetBTVariable<T>(string varName, T value)
        {
            if (BTAgent.GetVariable(varName, out BlackboardVariable<T> variable))
            {
                variable.Value = value;
                return true;
            }

            Debug.LogError($"{name}: 블랙보드 변수 {varName}가 없습니다.");
            return false;
        }

        protected bool GetBTVariable<T>(string varName, out BlackboardVariable<T> variable)
        {
            if (BTAgent.GetVariable(varName, out variable))
                return true;

            Debug.LogError($"{name}: 블랙보드 변수 {varName}가 없습니다.");
            return false;
        }

        #endregion
    }
}