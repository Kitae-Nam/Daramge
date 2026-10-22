using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 보스가 이동 시 나오는 파티클을 관리하는 스크립트입니다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BossMovementEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _stepParticle;

        private NavMeshAgent _agent;
        private bool isMove;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_stepParticle == null)
            {
                Debug.LogError($"파티클이 없습니다.");
                return;
            }
        }

        private void Update()
        {
            if (_stepParticle == null) return;

            bool movingNow =
                _agent.isActiveAndEnabled &&
                _agent.isOnNavMesh &&
                _agent.velocity.sqrMagnitude > 0.01f;
            
            if (movingNow == isMove) return;
            
            if (movingNow)
            {
                _stepParticle.Play();
            }
            else
            {
                // 파티클 재생 정지 여기서 True는 자식 파티클에도 적용한다는 뜻
                // ParticleSystemBehavior.StopEmitting은 새 입자 생성을 막는다는 뜻이다.
                _stepParticle.Stop(true,
                    ParticleSystemStopBehavior.StopEmitting);
            }

            isMove = movingNow;
        }
    }
}