using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Dash
{
    /// <summary>
    /// 보스가 돌진 시 남는 불 장판에 설치하는 불꽃을 설치하는 스크립트입니다.
    /// </summary>
    public sealed class DashScorchFlames : MonoBehaviour
    {
        private GameObject _prefab;
        private float _spacing;
        private float _heightOffset;
        private float _nextDistance;

        public void Initialize(GameObject prefab, float spacing, float heightOffset)
        {
            _prefab = prefab;
            _spacing = Mathf.Max(.1f,spacing);
            _heightOffset = heightOffset;
            _nextDistance = 0f;
        }

        public void ExtendTo(Vector3 start, Vector3 end)
        {
            if (_prefab == null) return;
            
            Vector3 horizontal = end - start;
            horizontal.y = 0f;
            
            float length = horizontal.magnitude;
            if (length < .0001f) return;
            while (_nextDistance <= length)
            {
                float ratio = _nextDistance / length;

                Vector3 position = Vector3.Lerp(start, end, ratio) + Vector3.up * _heightOffset;
                Instantiate(_prefab, position, _prefab.transform.rotation, transform);
                
                _nextDistance += _spacing;
            }
        }
    }
}