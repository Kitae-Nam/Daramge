using System;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 보스의 이동을 담당하는 스크립트입니다.
    /// 이동은 NavMeshAgent를 이용해 이동합니다.
    /// </summary>
    [RequireComponent(typeof(BossTarget), typeof(NavMeshAgent))]
    public class BossMovement : MonoBehaviour
    {
        private BossTarget _target;
        private NavMeshAgent _agent;
        private bool _isChase = true;

        private void Awake()
        {
            _target = GetComponent<BossTarget>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updateRotation = false;
        }

        private void Update()
        {
            if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled)
            {
                Debug.Log("에이전트가 NavMesh위에 없거나 활성화 되지 않았습니다.");
                return;
            }

            _agent.isStopped = !_isChase;
            if (!_isChase) return;

            if (!_target.HasTarget)
            {
                Debug.Log("타겟을 찾을 수 없습니다. 이동을 취소합니다.");
                _agent.ResetPath();
                return;
            }

            _agent.SetDestination(_target.Target.position);
        }

        public void SetChaseEnabled(bool isChase)
        {
            _isChase = isChase;
            // 안전 코드 추가 10월22일
            if (_agent == null || !_agent.isActiveAndEnabled
                               || !_agent.isOnNavMesh)
                return;

            if (_agent.isOnNavMesh && _agent.isActiveAndEnabled)
            {
                if (_isChase)
                {
                    Debug.Log("추적 가능 상태로 변경");
                    _agent.isStopped = false;
                }
                else
                {
                    Debug.Log("추적 불가 상태로 변경");
                    _agent.isStopped = true;
                }
            }
        }
    }
}