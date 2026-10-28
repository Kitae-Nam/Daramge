using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health
{
    /// <summary>그로기 시간과 취약 배율만 관리한다. 이동 정지/애니메이션은 이벤트 구독자의 책임이다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossHealth))]
    public sealed class BossGroggyController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _defaultDuration = 3f;
        [SerializeField, Min(1f)] private float _damageMultiplier = 1.5f;
        private BossHealth _health;
        private float _remainingTime;

        public bool IsGroggy { get; private set; }
        public float RemainingTime => _remainingTime;
        public event Action Entered;
        public event Action Exited;

        private void Awake() => _health = GetComponent<BossHealth>();
        private void OnEnable() => _health.Died += ExitGroggy;

        private void OnDisable()
        {
            _health.Died -= ExitGroggy;
            ExitGroggy();
        }

        private void Update()
        {
            if (!IsGroggy) return;
            _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
            if (_remainingTime <= 0f) ExitGroggy();
        }

        public bool TryEnterGroggy() => TryEnterGroggy(_defaultDuration);

        public bool TryEnterGroggy(float duration)
        {
            Debug.Log("TryEnterGroggy");
            if (!isActiveAndEnabled || _health.IsDead || duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                return false;

            // 재진입으로 배율이 누적되거나 남은 그로기 시간이 짧아지는 것을 방지한다.
            _remainingTime = Mathf.Max(_remainingTime, duration);
            if (IsGroggy) return true;

            IsGroggy = true;
            _health.SetDamageMultiplier(this, _damageMultiplier);
            Entered?.Invoke();
            return true;
        }

        public void ExitGroggy()
        {
            if (!IsGroggy) return;
            IsGroggy = false;
            _remainingTime = 0f;
            _health.RemoveDamageMultiplier(this);
            Exited?.Invoke();
        }
        
#if UNITY_EDITOR
        // 그로기 테스트용 에디터 함수  
        [ContextMenu("테스트/그로기 진입")]
        private void TestEnterGroggy()
        {
            if (!Application.isPlaying) return;

            TryEnterGroggy();
        }
#endif
    }
}
