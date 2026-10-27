using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Module
{
    //NavMeshAgent는 경로와 회전만, 실제 이동은 애니메이션 루트모션이 담당한다.
    //Animator와 같은 오브젝트(Visual)에 AgentRenderer 대신 붙인다.
    public class EnemyNavRenderer : AgentRenderer, IAfterInitModule
    {
        [SerializeField] private AnimParamSO speedParam;
        [SerializeField] private float speedSmoothTime = 0.1f;
        [Tooltip("루트모션이 NavMesh 밖으로 나가는지 검사하는 거리")]
        [SerializeField] private float navMeshSampleDistance = 0.5f;

        private NavMeshAgent _navAgent;
        private Vector2 _smoothDelta;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Debug.Assert(speedParam != null, $"Speed 파라미터가 없습니다. {gameObject.name}");
        }

        public void AfterInit()
        {
            INavMovement navMovement = _owner.GetModule<INavMovement>();
            Debug.Assert(navMovement != null, $"EnemyNavRenderer는 INavMovement가 필요합니다. {gameObject.name}");

            _navAgent = navMovement.NavAgent;
            _navAgent.updatePosition = false; //위치는 루트모션이 담당
            _navAgent.updateRotation = true;  //회전은 에이전트가 담당
        }

        private void Update()
        {
            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return;

            //에이전트가 가려는 위치와 실제 위치의 차이로 애니메이터 속도를 구한다.
            Vector3 worldDelta = _navAgent.nextPosition - _owner.transform.position;
            worldDelta.y = 0;

            Vector2 localDelta = new Vector2(
                Vector3.Dot(_owner.transform.right, worldDelta),
                Vector3.Dot(_owner.transform.forward, worldDelta));

            float smooth = Mathf.Min(1f, Time.deltaTime / speedSmoothTime);
            _smoothDelta = Vector2.Lerp(_smoothDelta, localDelta, smooth);
            float speed = Time.deltaTime > 0f ? _smoothDelta.magnitude / Time.deltaTime : 0f;

            if (speedParam != null)
                Animator.SetFloat(speedParam.ParamHash, speed);

            //애니메이션보다 에이전트가 너무 앞서가면 캐릭터 쪽으로 당겨온다.
            if (worldDelta.magnitude > _navAgent.radius)
                _navAgent.nextPosition = _owner.transform.position + worldDelta * 0.9f;
        }

        private void OnAnimatorMove()
        {
            if (_navAgent == null || !_navAgent.enabled) return;

            Vector3 rootPosition = Animator.rootPosition;
            rootPosition.y = _navAgent.nextPosition.y; //높이는 NavMesh 기준

            //공격 모션의 발 내딛기 등으로 NavMesh 밖으로 나가는 이동은 무시
            if (!NavMesh.SamplePosition(rootPosition, out _, navMeshSampleDistance, _navAgent.areaMask))
                return;

            _owner.transform.position = rootPosition;
            _navAgent.nextPosition = rootPosition;
        }
    }
}
