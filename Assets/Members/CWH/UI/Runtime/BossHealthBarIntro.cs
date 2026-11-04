using UnityEngine;

namespace Daramge.BossUI
{
    public sealed class BossHealthBarIntro
    {

        private readonly int _characterCount;
        private readonly float _fadeDuration;
        private readonly float _characterInterval;
        private readonly float _fillDuration;

        private float _elapsed;

        private float TypingDuration =>
            _characterCount * _characterInterval;

        private float FillStartTime =>
            _fadeDuration + TypingDuration;

        public float Opacity =>
            Mathf.Clamp01(_elapsed / _fadeDuration);

        public int VisibleCharacters =>
            Mathf.Clamp(
                Mathf.FloorToInt(
                    (_elapsed - _fadeDuration) / _characterInterval),
                0,
                _characterCount);

        public float FillProgress =>
            Mathf.Clamp01(
                (_elapsed - FillStartTime) / _fillDuration);

        public bool IsFilling =>
            _elapsed >= FillStartTime;

        public bool IsComplete =>
            _elapsed >= FillStartTime + _fillDuration;

        public BossHealthBarIntro(
            int characterCount,
            float fadeDuration,
            float characterInterval,
            float fillDuration)
        {
            _characterCount = Mathf.Max(0, characterCount);
            _fadeDuration = Mathf.Max(0.01f, fadeDuration);
            _characterInterval = Mathf.Max(0.01f, characterInterval);
            _fillDuration = Mathf.Max(0.01f, fillDuration);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += Mathf.Max(0f, deltaTime);
        }
    }
}