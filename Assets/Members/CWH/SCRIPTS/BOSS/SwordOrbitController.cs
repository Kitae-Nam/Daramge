using System.Collections.Generic;
using UnityEngine;

namespace Members.CWH.SCRIPTS.BOSS
{
    public class SwordOrbitController : MonoBehaviour
    {
        [Header("공전 기준점")]
        [SerializeField] private Transform _orbitCenter;

        [Header("공전할 검")]
        [SerializeField] private List<Transform> _swords = new List<Transform>();

        [Header("공전 설정")]
        [SerializeField] private float _orbitRadius = 2f;
        [SerializeField] private float _orbitSpeed = 30f;
        [SerializeField] private float _heightOffset = 0.3f;
        [SerializeField] private float _heightVariation = 0.25f;

        [Header("검 회전")]
        [SerializeField] private float _swordTilt = 15f;
        [SerializeField] private float _swordRotationOffset = 90f;

        private float _currentAngle;
        private bool _isOrbiting = true;

        private void Update()
        {
            if (_orbitCenter == null || !_isOrbiting) return;

            _currentAngle += _orbitSpeed * Time.deltaTime;

            for (int i = 0; i < _swords.Count; i++)
            {
                Transform sword = _swords[i];

                if (sword == null) continue;

                float angle = _currentAngle + i * (360f / _swords.Count);
                float radians = angle * Mathf.Deg2Rad;

                float x = Mathf.Cos(radians) * _orbitRadius;
                float z = Mathf.Sin(radians) * _orbitRadius;

                float height = _heightOffset
                               + Mathf.Sin(radians * 2f + i) * _heightVariation;

                sword.position = _orbitCenter.position
                                 + new Vector3(x, height, z);

                sword.rotation = Quaternion.Euler(
                    _swordTilt,
                    -angle + _swordRotationOffset,
                    0f
                );
            }
        }

        public void SetOrbiting(bool isOrbiting)
        {
            _isOrbiting = isOrbiting;
        }
    }
}