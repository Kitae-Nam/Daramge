using GGMLib.ModuleSystems;
using UnityEngine;

namespace NKT.Enemy.Module
{
    public class AgentSensor : MonoBehaviour, IModule
    {
        [SerializeField] private LayerMask whatIsTarget;
        [SerializeField] private LayerMask whatIsObstacle;
        [SerializeField] private int maxColliderCount = 5;
        [SerializeField] private float eyeHeight = 1;

        private ModuleOwner _owner;
        private Collider[] _colliderResults;
        
        public Collider[] ColliderResults => _colliderResults;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            Debug.Assert(maxColliderCount > 0, $"최대 컬라이더 카운트는 반드시 0보다 커야합니다. : {gameObject}");
            _colliderResults = new Collider[maxColliderCount];
        }

        public bool IsTargetInViewAngle(Transform targetTrm, float viewAngle)
        {
            Vector3 direction = targetTrm.position - transform.position;
            direction.y = 0;
            float angle = Vector3.Angle(transform.forward, direction);
            return angle <= viewAngle * 0.5f;
        }

        public bool IsTargetIsInSight(Transform targetTrm)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = targetTrm.position + Vector3.up * eyeHeight;
            Vector3 dir = target - origin;
            return !Physics.Raycast(origin, dir.normalized, dir.magnitude, whatIsObstacle);
        }
        
        public bool IsTargetInViewRadius(Transform targetTrm, float viewRadius)
            => (targetTrm.position - transform.position).sqrMagnitude <=  viewRadius * viewRadius;
        public int FindTargetsInRadius(float viewRadius)
            => Physics.OverlapSphereNonAlloc(transform.position, viewRadius, _colliderResults, whatIsTarget);

    }
}