using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>
    /// 직사각형 경고의 월드 위치/크기만 담당한다. 색과 진행도는 기존 Presenter에 맡긴다.
    /// 경고 동안 부모에서 분리해 보스 이동/회전/스케일이 이미 예고한 경로를 바꾸지 못하게 한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossTelegraphPresenter), typeof(MeshFilter))]
    public sealed class DashTelegraphPresenter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _heightOffset = 0.03f;
        private BossTelegraphPresenter _presenter;
        private Transform _originalParent;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;
        private bool _placed;

        private BossTelegraphPresenter Presenter => _presenter != null ? _presenter
            : (_presenter = GetComponent<BossTelegraphPresenter>());

        /// <summary>Unity 기본 Plane/Quad처럼 UV 축이 메시 축에 정렬된 평면을 경로에 맞춘다.</summary>
        public bool Show(DashPath path, float width, float opacity)
        {
            Hide();
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            Renderer meshRenderer = GetComponent<Renderer>();
            if (mesh == null || meshRenderer == null || path.Distance <= 0f || width <= 0f) return false;
            if (!TryGetUvBasis(mesh, out Vector3 u, out Vector3 v, out Vector3 localCenter)) return false;

            Material material = meshRenderer.sharedMaterial;
            float coverageU = GetCoverage(material, "_OutRectangleW");
            float coverageV = GetCoverage(material, "_OutRectangleH");
            int uAxis = DominantAxis(u);
            int vAxis = DominantAxis(v);
            if (uAxis == vAxis) return false;

            _originalParent = transform.parent;
            _originalPosition = transform.localPosition;
            _originalRotation = transform.localRotation;
            _originalScale = transform.localScale;
            _placed = true;
            transform.SetParent(null, true);

            // 실제 UV 방향을 읽으므로 Plane의 UV가 뒤집혀 있어도 채움 방향을 올바르게 맞춘다.
            Quaternion localFrame = Quaternion.LookRotation(v.normalized, Vector3.Cross(v, u).normalized);
            transform.rotation = Quaternion.LookRotation(path.Direction, Vector3.up) * Quaternion.Inverse(localFrame);
            Vector3 scale = Vector3.one;
            scale[uAxis] = width / (u.magnitude * coverageU);
            scale[vAxis] = path.Distance / (v.magnitude * coverageV);
            transform.localScale = scale;
            Vector3 center = path.Start + path.Direction * path.Distance * 0.5f + Vector3.up * _heightOffset;
            transform.position = center - transform.TransformVector(localCenter);

            Presenter.SetProgress(0f);
            Presenter.SetOpacity(opacity);
            return true;
        }

        public void SetProgress(float progress) => Presenter.SetProgress(progress);

        public void Hide()
        {
            if (Presenter != null) Presenter.SetOpacity(0f);
            if (!_placed) return;
            _placed = false;
            transform.SetParent(_originalParent, false);
            transform.localPosition = _originalPosition;
            transform.localRotation = _originalRotation;
            transform.localScale = _originalScale;
        }

        private static float GetCoverage(Material material, string property)
            => material != null && material.HasProperty(property)
                ? Mathf.Clamp(material.GetFloat(property), 0.01f, 1f) : 0.8f;

        private static int DominantAxis(Vector3 vector)
        {
            vector = new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
            return vector.x >= vector.y && vector.x >= vector.z ? 0 : vector.y >= vector.z ? 1 : 2;
        }

        private static bool TryGetUvBasis(Mesh mesh, out Vector3 u, out Vector3 v, out Vector3 center)
        {
            u = v = center = Vector3.zero;
            if (!mesh.isReadable) return false;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            int[] triangles = mesh.triangles;
            if (uv.Length != vertices.Length) return false;
            // UV와 정점의 차이로 UV를 1 증가시켰을 때의 로컬 이동 방향과 길이를 구한다.
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                Vector2 d1 = uv[b] - uv[a], d2 = uv[c] - uv[a];
                float determinant = d1.x * d2.y - d1.y * d2.x;
                if (Mathf.Abs(determinant) < 0.00001f) continue;
                Vector3 e1 = vertices[b] - vertices[a], e2 = vertices[c] - vertices[a];
                u = (e1 * d2.y - e2 * d1.y) / determinant;
                v = (e2 * d1.x - e1 * d2.x) / determinant;
                center = vertices[a] + u * (0.5f - uv[a].x) + v * (0.5f - uv[a].y);
                return u.sqrMagnitude > 0.0001f && v.sqrMagnitude > 0.0001f;
            }
            return false;
        }

        private void OnDisable() => Hide();
    }
}
