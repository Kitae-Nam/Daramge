using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 보스별 패턴의 확장 지점(OCP). 선택 로직은 이 계약에 의존하고 실제 공격은 파생 클래스가 맡는다.
    /// 경고 이벤트를 분리해 바닥 표시, 소리, 애니메이션을 Inspector에서 연결할 수 있다.
    /// </summary>
    public abstract class BossPattern : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _warningDuration = 1f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 1f;
        [SerializeField, Min(0f)] private float _cooldown = 5f;
        [SerializeField] private UnityEvent _warningStarted = new UnityEvent();
        [SerializeField] private UnityEvent _warningEnded = new UnityEvent();

        public float WarningDuration => Mathf.Max(0.01f, _warningDuration);
        public float RecoveryDuration => Mathf.Max(0f, _recoveryDuration);
        public float Cooldown => Mathf.Max(0f, _cooldown);

        // 거리/소환 기물 존재 여부 같은 조건은 각 패턴이 판단한다(ISP).
        public virtual bool CanExecute() => isActiveAndEnabled;
        public virtual void BeginWarning() => _warningStarted.Invoke();
        public virtual void EndWarning() => _warningEnded.Invoke();
        public abstract IEnumerator Execute();

        /// <summary>
        /// 정상 완료와 중단 모두에서 호출된다. 히트박스, 이동, 임시 이펙트를 여기서 해제한다.
        /// 반복 호출에도 안전하게 구현하고, 코루틴을 별도로 시작하지 말고 Execute에서 yield한다.
        /// </summary>
        public virtual void Cleanup() { }
        
        /// <summary>
        /// 러너가 계산한 경고 진행도(0~1)를 전달받는다.
        /// 표시가 필요한 패턴에서 재정의한다.
        /// </summary>
        /// <param name="progress"></param>
        public virtual void UpdateWarningProgress(float progress) { }
    }
}
