using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs
{
    /// <summary>
    /// 실제 월드 위치 변화에 맞춰 이동 파티클만 제어한다.
    /// 이동 방식/NavMesh 속도에 의존하지 않으므로 일반 추적과 그리드 이동 모두에 사용할 수 있다.
    /// </summary>
    public class BossMovementEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _stepParticle;

        [SerializeField, Min(0.001f)] private float _minimumSpeed = 0.1f;
        private Vector3 _previousPosition;
        private bool isMove;

        private void Awake()
        {
            if (_stepParticle == null)
            {
                Debug.LogError($"파티클이 없습니다.");
                return;
            }
        }

        private void OnEnable()
        {
            // 비활성화 중의 위치 변경을 이동으로 오인하지 않도록 기준점을 다시 잡는다.
            _previousPosition = transform.position;
            isMove = false;
            StopEmitting();
        }

        private void LateUpdate()
        {
            // 이동기의 Update가 끝난 뒤 검사한다. Agent.Move로 이동하면 Agent.velocity는 0일 수 있다.
            Vector3 displacement = transform.position - _previousPosition;
            _previousPosition = transform.position;
            displacement.y = 0f; // 제자리 상하 연출은 발의 수평 이동으로 세지 않는다.
            if (_stepParticle == null) return;

            float deltaTime = Time.deltaTime;
            bool movingNow = deltaTime > 0f &&
                displacement.sqrMagnitude > _minimumSpeed * _minimumSpeed * deltaTime * deltaTime;
            
            if (movingNow == isMove) return;
            
            if (movingNow)
            {
                _stepParticle.Play();
            }
            else
            {
                StopEmitting();
            }

            isMove = movingNow;
        }

        private void StopEmitting()
        {
            // 새 입자만 멈춘다. 이미 나온 먼지는 원래 수명대로 사라지게 둔다.
            if (_stepParticle != null) _stepParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDisable() { isMove = false; StopEmitting(); }
    }
}
