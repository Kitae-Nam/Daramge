using System;

namespace Daramge.BossUI
{
    /// <summary>잔상 시간 계산만 담당한다. UI와 Unity 시간에 의존하지 않아 독립 검증이 가능하다.</summary>
    public sealed class HealthBarLag
    {
        private float _delayRemaining;
        public float Current { get; private set; } = 1f;
        public float Trail { get; private set; } = 1f;

        public void SetTarget(float ratio, float delay, bool immediate = false)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio))
                throw new ArgumentOutOfRangeException(nameof(ratio));

            ratio = Math.Max(0f, Math.Min(1f, ratio));
            // 회복/보스 교체 시 과거 피해 잔상을 제거해 실제 체력보다 짧은 잔상이 생기지 않게 한다.
            if (immediate || ratio > Current)
            {
                Trail = ratio;
                _delayRemaining = 0f;
            }
            else if (ratio < Current)
            {
                _delayRemaining = Math.Max(0f, delay);
            }
            Current = ratio;
        }

        public void Tick(float deltaTime, float catchUpDuration)
        {
            float elapsed = Math.Max(0f, deltaTime);
            // 지연이 끝난 프레임에서도 남은 시간만 사용해 프레임률에 따른 차이를 줄인다.
            float movementTime = Math.Max(0f, elapsed - _delayRemaining);
            _delayRemaining = Math.Max(0f, _delayRemaining - elapsed);
            if (movementTime <= 0f) return;

            // 전체 게이지 기준 일정 속도로 이동하므로 연속 피해에도 튀지 않고 반드시 목표에 도달한다.
            Trail = catchUpDuration <= 0f
                ? Current
                : Math.Max(Current, Trail - movementTime / catchUpDuration);
        }
    }
}
