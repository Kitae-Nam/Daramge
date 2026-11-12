using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Dash
{
    public interface IDashScorchVisual
    {
        void SetSegment(Vector3 start, Vector3 end, float width);
    }

    /// <summary>장판 메시와 머티리얼만 관리한다. 피해 검사/수명/페이즈에 의존하지 않는다.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class DashScorchVisual : MonoBehaviour, IDashScorchVisual
    {
        private Mesh _mesh;
        private DashScorchFlames _flames;

        public void Initialize(Material material, DashScorchFlames flames = null)
        {
            {
                _mesh = new Mesh { name = "Actual dash strip" };
                _flames = flames;
            }

            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        public void SetSegment(Vector3 start, Vector3 end, float width)
        {
            Vector3 forward = end - start;
            forward.y = 0f;
            Vector3 side = forward.sqrMagnitude > 0.0001f
                ? Vector3.Cross(Vector3.up, forward.normalized) * (width * 0.5f)
                : Vector3.right * (width * 0.5f);
            Vector3 lift = Vector3.up * 0.08f;
            _mesh.Clear();
            _mesh.vertices = new[] { start - side + lift, start + side + lift, end - side + lift, end + side + lift };
            _mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            _mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            _mesh.RecalculateBounds();
            
            if(_flames != null)
                _flames.ExtendTo(start, end);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}