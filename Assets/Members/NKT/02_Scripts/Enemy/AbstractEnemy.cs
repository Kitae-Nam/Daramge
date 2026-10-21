using Members.CHG._02_Scripts.Agent;
using NKT.Enemy.Module;
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
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            NavMovement = GetModule<INavMovement>();
            Renderer = GetModule<IRenderer>();
            AgentSensor = GetModule<AgentSensor>();
            AgentTrigger = GetModule<AgentTrigger>();
            BTAgent = GetModule<BehaviorGraphAgent>();
        }

        protected override void HandleHitEvent()
        {
            if (IsDead) return;
            //BT Hit로 연결 
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            foreach (Collider c in GetComponents<Collider>())
                c.enabled = false;
            //BT Dead로 연결
        }
    }
}