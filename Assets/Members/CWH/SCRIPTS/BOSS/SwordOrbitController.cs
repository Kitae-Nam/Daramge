using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using UnityEngine;

namespace CWH.SCRIPTS.BOSS
{
    /// <summary>
    /// 검 회전 기능 스크립트
    /// </summary>
    public class SwordOrbitController : MonoBehaviour
    {
        [Header("공전 기준점")] [SerializeField] private Transform _orbitCenter;

        [Header("공전할 검")] [SerializeField] private BossSwordInventory _inventory;

        [Header("공전 설정")] [SerializeField, Min(0.01f)]
        private float _orbitRadius = 2f;

        [SerializeField, Min(0.01f)] private float _orbitSpeed = 30f;
        [SerializeField, Min(0.01f)] private float _heightOffset = 0.3f;
        [SerializeField, Min(0.01f)] private float _heightVariation = 0.25f;

        [Header("검 회전")] [SerializeField] private float _swordTilt = 15f;
        [SerializeField] private float _swordRotationOffset = 90f;

        private float _currentAngle;
        private bool _isOrbiting = true;

        private void Update()
        {
            if (_orbitCenter == null || !_isOrbiting || _inventory == null) return;
            if (_inventory.HasHeldSword) return;
            
            // var를 쓴이유 어차피 BossSword인데 또 쓰면 길어지잖아 ㅋㅋ 귀찮아
            var _swords = _inventory.Swords;

            if (_swords.Count <= 0) return;

            _currentAngle += _orbitSpeed * Time.deltaTime;

            for (int i = 0; i < _swords.Count; i++)
            {
                BossSword BossSword = _swords[i];

                if (BossSword == null) continue;
                
                // 공전 중인 검만 사용한다. / 공전 중이지 않는 검을 사용하지 않기 위한 예방 코드
                if(BossSword.State != BossSwordState.Orbiting) continue;
                
                Transform sword = BossSword.transform;
                
                float angle = _currentAngle + i * (360f / _swords.Count);
                float radians = angle * Mathf.Deg2Rad;
                
                float x = Mathf.Cos(radians) * _orbitRadius;
                float z = Mathf.Sin(radians) * _orbitRadius;

                float height = _heightOffset +
                               Mathf.Sin(radians * 2f + i) * _heightVariation;

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