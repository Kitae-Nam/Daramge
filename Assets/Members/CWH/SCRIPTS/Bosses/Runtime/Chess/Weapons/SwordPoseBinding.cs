using System;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>
    /// 검의 원래 부모/자세 저장과 복원만 담당한다. 근접과 투척이 같은 복귀 규칙을 공유한다.
    /// 보스가 비활성화되는 중에는 부모 변경을 다음 안전한 프레임으로 미룬다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwordPoseBinding : MonoBehaviour
    {
        private Transform _parent;
        private bool _hadParent;
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private Renderer[] _renderers;
        private bool[] _visibility;
        private bool _keepWorldPoseOnReturn;
        public bool IsCaptured { get; private set; }
        public bool IsReturnPending { get; private set; }
        public event Action Returned;

        public bool TryCapture()
        {
            if (IsCaptured) return false;
            _parent = transform.parent;
            _hadParent = _parent != null;
            _position = transform.localPosition;
            _rotation = transform.localRotation;
            _scale = transform.localScale;
            _keepWorldPoseOnReturn = false;
            IsCaptured = true;
            return true;
        }

        public void Restore(bool deferParenting = false)
        {
            if (!IsCaptured || IsReturnPending) return;
            if (!deferParenting) { CompleteReturn(); return; }
            IsReturnPending = true;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _visibility = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _visibility[i] = _renderers[i].enabled;
                _renderers[i].enabled = false;
            }
        }
        
        public void KeepCurrentWorldPoseOnReturn()
        {
            if (IsCaptured && !IsReturnPending)
                _keepWorldPoseOnReturn = true;
        }

        private void Update()
        {
            if (!IsReturnPending) return;
            if (_hadParent && _parent == null) { Destroy(gameObject); return; }
            if (_parent != null && !_parent.gameObject.activeInHierarchy) return;
            CompleteReturn();
        }

        private void CompleteReturn()
        {
            Vector3 worldPosition = transform.position;
            Quaternion worldRotation = transform.rotation;
            transform.SetParent(_parent, false);
            transform.localPosition = _position;
            transform.localRotation = _rotation;
            transform.localScale = _scale;
            if (_keepWorldPoseOnReturn)
                transform.SetPositionAndRotation(worldPosition, worldRotation);
            _keepWorldPoseOnReturn = false;
            if (IsReturnPending)
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null) _renderers[i].enabled = _visibility[i];
            IsCaptured = IsReturnPending = false;
            _parent = null;
            Returned?.Invoke();
        }
    }
}
