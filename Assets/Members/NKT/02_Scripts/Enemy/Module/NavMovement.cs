using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Module
{
    public class NavMovement : MonoBehaviour, INavMovement, IModule
    {
        public NavMeshAgent NavAgent { get; private set; }

        public Vector3 Velocity
        {
            get => NavAgent != null ? NavAgent.velocity : Vector3.zero;
            set
            {
                if(NavAgent != null)
                    NavAgent.velocity = value;
            }
        }

        public float Speed
        {
            get => NavAgent != null ? NavAgent.speed : 0f;
            set
            {
                if(NavAgent != null)
                    NavAgent.speed = value;
            }
        }

        public bool IsStopped
        {
            get => NavAgent != null && NavAgent.isStopped;
            set
            {
                if(NavAgent != null)
                    NavAgent.isStopped = value;
            }
        }
        public bool IsArrived 
            => NavAgent != null
               && (!NavAgent.pathPending && NavAgent.remainingDistance <= NavAgent.stoppingDistance * 0.5f);
        
        public void Initialize(ModuleOwner owner)
        {
            NavAgent = owner.GetComponent<NavMeshAgent>();
            Debug.Assert(NavAgent != null, $"NavMovement는 반드시 owner NavMeshAgent가 있어야 합니다. {gameObject.name}");
        }
        
        public void SetDestination(Vector3 destination)
        {
            NavAgent.SetDestination(destination);
        }
    }
}