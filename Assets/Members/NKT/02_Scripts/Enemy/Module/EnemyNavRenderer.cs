using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using GGMLib.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Module
{
    //수업 버전 NavAgentRenderer 방식.
    //NavMeshAgent는 경로를 계산하고, 실제 이동은 애니메이션 루트모션이 담당한다.
    //Animator와 같은 오브젝트(Visual)에 AgentRenderer 대신 붙인다.
    public class EnemyNavRenderer : AgentRenderer, IAfterInitModule
    {
        [SerializeField] private AnimParamSO speedParam;
        [SerializeField] private float speedSmoothTime = 0.1f;
        [Tooltip("루트모션이 NavMesh 밖으로 나가는지 검사하는 거리")]
        [SerializeField] private float navMeshSampleDistance = 0.5f;

        [Header("NavAgent의 position및 rotation 제어여부")]
        [SerializeField] private bool updateRotation = true;
        [SerializeField] private bool updatePosition = false;

        [Header("NavAgent의 회전을 무시하고 강제로 회전시키기")]
        [SerializeField] private bool forceRotation;
        [SerializeField] private float forceRotationSpeed = 540f;

        private INavMovement _navMovement;
        private NavMeshAgent _navAgent;

        private Vector2 _velocity;
        private Vector2 _smoothDeltaPosition;

        public bool UpdateRotationByAnimator
        {
            get => !updateRotation;
            set
            {
                updateRotation = !value;
                if (_navAgent != null)
                    _navAgent.updateRotation = updateRotation;
            }
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _navMovement = owner.GetModule<INavMovement>();
            Debug.Assert(_navMovement != null, $"EnemyNavRenderer는 INavMovement가 필요합니다. {gameObject.name}");
            Debug.Assert(speedParam != null, $"Speed 파라미터가 없습니다. {gameObject.name}");
        }

        public void AfterInit()
        {
            _navAgent = _navMovement.NavAgent;
            _navAgent.updatePosition = updatePosition;
            _navAgent.updateRotation = updateRotation;
        }

        private void OnAnimatorMove()
        {
            if (_navAgent == null || !_navAgent.enabled) return;

            Vector3 rootPosition = Animator.rootPosition;
            rootPosition.y = _navAgent.nextPosition.y; //다음 좌표의 y값을 가져온다.

            if (NavMesh.SamplePosition(rootPosition, out NavMeshHit hit, navMeshSampleDistance, _navAgent.areaMask))
            {
                _navAgent.nextPosition = rootPosition;
                _owner.transform.position = rootPosition;
            }

            if (UpdateRotationByAnimator)
                _owner.transform.rotation = Animator.rootRotation;
        }

        private void Update()
        {
            SynchronizeAnimatorAndNavAgent();
            ForceRotationControl();
        }

        private void SynchronizeAnimatorAndNavAgent()
        {
            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return;

            Vector3 worldDeltaPosition = _navAgent.nextPosition - _owner.transform.position;
            worldDeltaPosition.y = 0; //y축은 자동 보정을 따라갈거라 냅두고

            float dx = Vector3.Dot(_owner.transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(_owner.transform.forward, worldDeltaPosition);

            Vector2 deltaPosition = new Vector2(dx, dy);
            float smooth = Mathf.Min(1, Time.deltaTime / speedSmoothTime);

            _smoothDeltaPosition = Vector2.Lerp(_smoothDeltaPosition, deltaPosition, smooth);
            _velocity = Time.deltaTime > 0f ? _smoothDeltaPosition / Time.deltaTime : Vector2.zero;

            //천천히 감속시작. stoppingDistance가 0이면 0으로 나누게 되므로 건너뛴다.
            if (_navAgent.stoppingDistance > 0f && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                _velocity = Vector2.Lerp(Vector2.zero, _velocity,
                    _navAgent.remainingDistance / _navAgent.stoppingDistance);
            }

            if (speedParam != null)
                Animator.SetFloat(speedParam.ParamHash, _velocity.magnitude);

            //에이전트와 너무 벌어지면 캐릭터를 에이전트 쪽으로 당긴다.
            float deltaMagnitude = worldDeltaPosition.magnitude;
            if (deltaMagnitude > _navAgent.radius * 0.5f)
            {
                _owner.transform.position = Vector3.Lerp(Animator.rootPosition, _navAgent.nextPosition, smooth);
            }
        }

        private void ForceRotationControl()
        {
            if (!forceRotation || _navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh || _navMovement.IsArrived)
                return;

            Vector3 desiredDirection = _navAgent.steeringTarget - _owner.transform.position; //다음 바라보는 지점
            desiredDirection.y = 0;
            if (desiredDirection.sqrMagnitude < 0.01f) return; //완전 정지상태에 가깝다면 회전안함.

            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection.normalized);
            _owner.transform.rotation = Quaternion.RotateTowards(
                _owner.transform.rotation, targetRotation, forceRotationSpeed * Time.deltaTime);
        }
    }
}
