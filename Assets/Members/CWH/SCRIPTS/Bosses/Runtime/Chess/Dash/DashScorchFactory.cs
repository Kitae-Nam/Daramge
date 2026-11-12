using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Dash
{
    /// <summary>장판의 표시와 판정 컴포넌트를 조립한다. 패턴이 렌더링 생성 방식을 알 필요가 없게 한다.</summary>
    [DisallowMultipleComponent]
    public sealed class DashScorchFactory : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private GameObject _flamePrefab;
        [SerializeField, Min(.1f)] private float _widthMultiplier = 1f;
        [SerializeField, Min(.1f)] private float _flameSpacing = 1f;
        [SerializeField] private float _flameHeightOffset = 0.08f;
        
        public DashScorchField Spawn(Transform target, float width, ScorchSettings settings)
        {
            float scorchWidth = Mathf.Max(.1f,width * _widthMultiplier);
            var root = new GameObject("Phase two dash scorch");
            var flames = root.AddComponent<DashScorchFlames>();
            flames.Initialize(
                _flamePrefab,
                _flameSpacing,
                _flameHeightOffset);
            
            var visual = root.AddComponent<DashScorchVisual>();
            visual.Initialize(_material, flames);
            
            var field = root.AddComponent<DashScorchField>();
            field.Initialize(transform, target, scorchWidth, settings, visual);
            
            return field;
        }
    }
}
