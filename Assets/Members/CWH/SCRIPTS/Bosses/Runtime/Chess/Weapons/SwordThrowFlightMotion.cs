using System;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    public sealed class SwordThrowFlightMotion : MonoBehaviour
    {
        [SerializeField, Min(.0f)] private float _riseHeight = 12f;
        [SerializeField, Min(.0f)] private float _riseDuration = .15f;
        [SerializeField, Min(.0f)] private float _transferDuration = .15f;


        private Transform _visual;
        private Vector3 _start;
        private Vector3 _riseEnd;
        private Vector3 _fallStart;
        private Vector3 _landing;

        private Quaternion _startRotation;
        private Quaternion _fallRotation;
        private float _fallDuration;

        public void Begin(Vector3 start, Vector3 landing, Transform visual, float fallDuration)
        {
            _start = start;
            _landing = landing;
            _visual = visual;
            _fallDuration = Mathf.Max(.03f, fallDuration);

            float topY = Mathf.Max(start.y, landing.y) + _riseHeight;
            _riseEnd = new Vector3(start.x, topY, start.z);
            _fallStart = new Vector3(landing.x, topY, landing.z);

            _startRotation = visual != null ? visual.rotation : Quaternion.identity;
            _fallRotation =
                Quaternion.FromToRotation(
                    _startRotation * Vector3.up, Vector3.down) * _startRotation;
        }

        public bool Tick(float elapsed)
        {
            float riseTime = Mathf.Max(.01f, _riseDuration);
            float transferTime = Math.Max(.01f, _transferDuration);

            if (elapsed < riseTime)
            {
                float t = Mathf.Clamp01(elapsed / riseTime);
                transform.position = Vector3.Lerp(_start, _riseEnd, t);
                SetVisualRotation(_startRotation);
                return false;
            }

            elapsed -= riseTime;

            if (elapsed < transferTime)
            {
                float t = Mathf.Clamp01(elapsed / transferTime);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(_riseEnd, _fallStart, smooth);
                SetVisualRotation(Quaternion.Slerp(_startRotation, _fallRotation, smooth));
                return false;
            }

            elapsed -= transferTime;

            float fallProgress =
                Mathf.Clamp01(elapsed / _fallDuration);
            transform.position = Vector3.Lerp(_fallStart, _landing, fallProgress * fallProgress);
            SetVisualRotation(_fallRotation);
            return fallProgress >= 1f;
        }

        private void SetVisualRotation(Quaternion rotation)
        {
            if(_visual != null)
                _visual.rotation = rotation;
        }
    }
}