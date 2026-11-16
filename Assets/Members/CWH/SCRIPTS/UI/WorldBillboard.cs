using UnityEngine;

namespace Daramge.BossUI.Members.CWH.SCRIPTS.UI
{
    /// <summary>
    /// 월드 기준 위치와 카메라 방향을 따른다. 부모의 확대/회전과 UI 수명을 분리한다.
    /// </summary>
    public class WorldBillboard : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0f, 0f);
        private Camera _camera;
        private bool _hasHadTarget;

        private void Awake()
        {
            _camera = Camera.main;
            ConfigureTarget();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            ConfigureTarget();
        }

        private void ConfigureTarget()
        {
            if (_target == null) return;
            _hasHadTarget = true;
            // UI에 작성한 로컬 크기를 월드 크기로 사용해 검 차징 배율과 비균일 스케일을 받지 않는다.
            if (Application.isPlaying && transform.parent != null) transform.SetParent(null, false);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                // 분리한 UI도 검 파괴/보스 사망 때 남지 않도록 추적 대상과 함께 정리한다.
                if (_hasHadTarget) Destroy(gameObject);
                return;
            }
            

            transform.position = _target.position + _worldOffset;

            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return;
            transform.rotation = _camera.transform.rotation;
        }
    }
}
