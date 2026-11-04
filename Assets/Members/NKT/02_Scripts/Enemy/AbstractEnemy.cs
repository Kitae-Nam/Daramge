using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using GGMLib.ObjectPool.Runtime;
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
    public abstract class AbstractEnemy : Agent, IPoolable
    {
        [field:SerializeField] public EnemyDataSO EnemyData {get; private set;}
        
        public INavMovement NavMovement { get; private set; }
        public IRenderer Renderer { get; private set; }
        public AgentSensor AgentSensor { get; private set; }
        public AgentTrigger AgentTrigger { get; private set; }
        public BehaviorGraphAgent BTAgent { get; private set; }
        public StateChannel  StateChannel { get; private set; }
        public EnemySkillModule SkillModule { get; private set; }
        public AbstractHealthModule HealthModule { get; private set; }
        public Vector3 HomePoint => _homePoint;

        private Vector3 _homePoint;
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            NavMovement = GetModule<INavMovement>();
            Renderer = GetModule<IRenderer>();
            AgentSensor = GetModule<AgentSensor>();
            AgentTrigger = GetModule<AgentTrigger>();
            SkillModule = GetModule<EnemySkillModule>();
            HealthModule =  GetModule<HealthModule>();
            
            BTAgent = GetComponent<BehaviorGraphAgent>();
        }

        public virtual void Start()
        {
            SetBTVariable(BTVar.Enemy, this);

            if (GetBTVariable(BTVar.StateChannel, out BlackboardVariable<StateChannel> channel))
                StateChannel = channel.Value;

            if (_homePoint == Vector3.zero) //스포너가 PlaceAt으로 정했으면 덮어쓰지 않는다.
                _homePoint = transform.position;
        }

        protected override void HandleHitEvent()
        {
            if (IsDead) return;
            if (SkillModule != null && SkillModule.IsArmored) return; //데미지는 들어가고 경직만 무시

            //쓰던 스킬의 애니메이션 이벤트 구독을 끊어야, 맞는 중에 공격 판정/발사가 나가거나
            //HIT 클립의 끝 이벤트로 스킬 종료가 불리지 않는다.
            if (SkillModule != null)
                SkillModule.StopSkillIfNotFinished();

            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = false;
            if (SkillModule != null)
                SkillModule.StopSkillIfNotFinished();
            StateChannel?.SendEventMessage(EnemyState.DEATH);
        }
        #region Pool
        [field:SerializeField] public PoolItemSO PoolItem { get; set; }
        public GameObject GameObject => this != null ?  this.gameObject : null;
        public virtual void ResetItem()
        {
            HealthModule.ResetHealth();
            IsDead = false;
            RemoveStatus(Status);
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = true;
            SkillModule.StopSkillIfNotFinished();

            //자세를 먼저 초기화하고 BT를 재시작해야, BT가 보낸 첫 애니메이션이 Rebind에 지워지지 않는다.
            Renderer.Animator.Rebind();
            //처음 꺼낼 때는 Start보다 먼저 그래프가 돌 수 있으므로 여기서도 Enemy를 넣어준다.
            SetBTVariable(BTVar.Enemy, this);
            SetBTVariable<GameObject>(BTVar.TargetGameObject, null);
            BTAgent.Restart();
        }

        //스포너가 풀에서 꺼낸 직후 호출. 위치와 집(배회 기준점)을 정한다.
        public bool PlaceAt(Vector3 position, Quaternion rotation, float sampleDistance = 2f)
        {
            NavMeshAgent agent = NavMovement.NavAgent;
            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };

            //자기 Agent Type의 NavMesh 위로 보정 (비행 적은 비행 NavMesh)
            bool isOnNavMesh = NavMesh.SamplePosition(position, out NavMeshHit hit, sampleDistance, filter);
            Vector3 spawnPosition = isOnNavMesh ? hit.position : position;

            //에이전트는 자기 위치를 따로 기억하므로 Warp로 옮기고, 루트모션 렌더러가 되돌리지 않게 transform도 같이 옮긴다.
            if (agent.isActiveAndEnabled)
                agent.Warp(spawnPosition);
            transform.SetPositionAndRotation(spawnPosition, rotation);
            _homePoint = spawnPosition;

            if (!isOnNavMesh)
                Debug.LogWarning($"{name}: 스폰 위치 근처에 NavMesh가 없습니다. {position}", this);
            return isOnNavMesh;
        }
        #endregion
        
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