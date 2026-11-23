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
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            AbstractEnemy enemy = GetComponentInParent<AbstractEnemy>();
            if (enemy == null || enemy.EnemyData == null) return;

            float radius = enemy.EnemyData.DetectRadius;
            float angle = enemy.EnemyData.ViewAngle;
            Vector3 from = Quaternion.Euler(0f, -angle * 0.5f, 0f) * transform.forward; //부채꼴 왼쪽 끝

            UnityEditor.Handles.color = new Color(1f, 0.8f, 0f, 0.15f);
            UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, from, angle, radius); //시야각
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, radius);              //감지 반경
        }
#endif
    }
}