using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>장판의 표시와 판정 컴포넌트를 조립한다. 패턴이 렌더링 생성 방식을 알 필요가 없게 한다.</summary>
    [DisallowMultipleComponent]
    public sealed class DashScorchFactory : MonoBehaviour
    {
        [SerializeField] private Material _material;
        public DashScorchField Spawn(Transform target, float width, ScorchSettings settings)
        {
            var root = new GameObject("Phase two dash scorch");
            var visual = root.AddComponent<DashScorchVisual>();
            visual.Initialize(_material);
            var field = root.AddComponent<DashScorchField>();
            field.Initialize(transform, target, width, settings, visual);
            return field;
        }
    }
}
