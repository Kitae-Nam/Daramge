using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>연출과 판정이 함께 읽는 동작 데이터다. Transform/체력/이펙트를 직접 변경하지 않는다.</summary>
    [Serializable]
    public sealed class SwordSwingMotion
    {
        [SerializeField, Range(0.1f, 1f)] private float _chargeMotionFraction = 0.8f;
        [SerializeField, Min(0.01f)] private float _chargeAngle = 100f;
        [SerializeField, Min(0.01f)] private float _finishAngle = 80f;

        public float ChargeFraction(float progress) => Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(progress / Mathf.Max(0.1f, _chargeMotionFraction)));
        public float ChargeAngle(int strike) => -_chargeAngle * Direction(strike);
        public float Angle(float progress, int strike) => Mathf.Lerp(-_chargeAngle, _finishAngle,
            EvaluateSlash(progress)) * Direction(strike);
        public bool HasCrossedFront(float progress) => EvaluateSlash(progress) >=
            _chargeAngle / Mathf.Max(0.01f, _chargeAngle + _finishAngle);
        private static float Direction(int strike) => strike % 2 == 0 ? 1f : -1f;
        private static float EvaluateSlash(float progress)
        {
            float remaining = 1f - Mathf.Clamp01(progress);
            return 1f - remaining * remaining;
        }
    }
}
