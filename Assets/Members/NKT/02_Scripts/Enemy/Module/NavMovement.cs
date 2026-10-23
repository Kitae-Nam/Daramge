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

        private NavMeshPath _path;
        
        public void Initialize(ModuleOwner owner)
        {
            NavAgent = owner.GetComponent<NavMeshAgent>();
            Debug.Assert(NavAgent != null, $"NavMovement는 반드시 owner NavMeshAgent가 있어야 합니다. {gameObject.name}");
            _path = new NavMeshPath();
        }
        
        public void SetDestination(Vector3 destination)
        {
            NavAgent.SetDestination(destination);
        }

        public bool TryGetRandomPoint(
            Vector3 center, float radius, float min, out Vector3 result, int tryCount = 10)
        {
            for (int i = 0; i < tryCount; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(circle.x, 0 , circle.y);
                
                if(!NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavAgent.height, NavAgent.areaMask))
                    continue;
                if ((hit.position - NavAgent.transform.position).sqrMagnitude < min * min)
                    continue;
                if(!NavAgent.CalculatePath(hit.position, _path) || _path.status != NavMeshPathStatus.PathComplete)
                    continue;
                
                result = hit.position;
                return true;
            }
            result = center;
            return false;
        }
    }
}