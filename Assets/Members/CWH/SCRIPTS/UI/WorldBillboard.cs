using UnityEngine;

namespace Daramge.BossUI.Members.CWH.SCRIPTS.UI
{
    /// <summary>
    /// 검이 바닥에 꽃히면 UI를 어쩌구 할거임
    /// </summary>
    public class WorldBillboard : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0f, 0f);
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            

            transform.position = _target.position + _worldOffset;

            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return;
            transform.rotation = _camera.transform.rotation;
        }
    }
}