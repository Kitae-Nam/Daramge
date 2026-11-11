using UnityEngine;

namespace NKT.Enemy.UI
{
    [DisallowMultipleComponent]
    public class Billboard : MonoBehaviour
    {
        public enum Mode
        {
            FullFaceCamera,
            YAxisOnly
        }

        [SerializeField] private Mode mode = Mode.YAxisOnly;

        private Transform _cam;

        private void OnEnable()
        {
            if(Camera.main != null) 
                _cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                if (Camera.main == null) return;
                _cam = Camera.main.transform;
            }

            if (mode == Mode.FullFaceCamera)
            {
                //카메라가 바라보는 방향과 같은 회전을 해서 화면에 보이도록 한다.
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
            }
            else
            {
                Vector3 dir = transform.position - _cam.position;
                dir.y = 0f;
                if(dir.sqrMagnitude > 1e-4f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}