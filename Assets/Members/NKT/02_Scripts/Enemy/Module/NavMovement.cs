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
            //자기 Agent Type의 NavMesh에서만 찾는다. (지상 적은 지상, 비행 적은 비행 NavMesh)
            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = NavAgent.agentTypeID,
                areaMask = NavAgent.areaMask
            };
            //비행 적은 기준점이 baseOffset만큼 떠 있으므로 그만큼 더 아래까지 찾는다.
            float sampleDistance = NavAgent.height + NavAgent.baseOffset;

            for (int i = 0; i < tryCount; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(circle.x, 0 , circle.y);

                if(!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleDistance, filter))
                    continue;

                Vector3 flatDiff = hit.position - NavAgent.transform.position;
                flatDiff.y = 0; //높이 차이는 이동 거리에서 제외
                if (flatDiff.sqrMagnitude < min * min)
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