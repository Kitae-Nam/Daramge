using UnityEngine;
using UnityEngine.Rendering;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs
{
    /// <summary>
    /// 기존 원형 Shader Graph를 부채꼴 메시 위에 그린다. 공격 판정과 같은 반지름/각도를 받는다.
    /// 진행도/투명도는 기존 Presenter에 맡기며 메시를 매 프레임 다시 만들지 않는다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BossTelegraphPresenter))]
    public sealed class AreaTelegraphPresenter : MonoBehaviour
    {
        [SerializeField] private float _heightOffset = 0.06f;
        private Mesh _mesh;
        private BossTelegraphPresenter _presenter;
        private bool _visible;
        private Vector3 _worldPosition;
        private Quaternion _worldRotation;

        public static AreaTelegraphPresenter Create(Transform parent, Material material, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var area = root.AddComponent<AreaTelegraphPresenter>();
            area.GetComponent<Renderer>().sharedMaterial = material;
            return area;
        }

        public void Show(Vector3 origin, Vector3 forward, float radius, float angle = 360f)
        {
            if (_presenter == null)
            {
                _presenter = GetComponent<BossTelegraphPresenter>();
                _presenter.ConfigureRenderer(GetComponent<Renderer>());
            }
            Renderer renderer = GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (_mesh == null) { _mesh = new Mesh { name = "Attack sector warning" }; GetComponent<MeshFilter>().sharedMesh = _mesh; }
            angle = Mathf.Clamp(angle, 1f, 360f);
            int segments = Mathf.Max(4, Mathf.CeilToInt(angle / 5f));
            var vertices = new Vector3[segments + 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 3];
            uv[0] = Vector2.one * 0.5f;
            Material material = renderer.sharedMaterial;
            float edge = material != null && material.HasProperty("_OutLineEdge")
                ? material.GetFloat("_OutLineEdge") : 0.4f;
            for (int i = 0; i <= segments; i++)
            {
                float radians = (-angle * 0.5f + angle * i / segments) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                vertices[i + 1] = radial * radius;
                // 아주 작은 여유로 Step 경계에서 테두리가 잘리는 것을 방지한다.
                uv[i + 1] = uv[0] + new Vector2(radial.x, radial.z) * Mathf.Max(0.01f, edge - 0.0001f);
                if (i == segments) continue;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            // 부모 보스의 스케일이 실제 경고 반지름을 바꾸지 않도록 월드 크기로 보정한다.
            _worldPosition = origin + Vector3.up * _heightOffset;
            _worldRotation = Quaternion.LookRotation(forward.sqrMagnitude > 0.0001f ? forward : Vector3.forward);
            _visible = true;
            ApplyWorldPose();
            _presenter.SetProgress(0f);
            _presenter.SetOpacity(0.7f);
        }

        private void ApplyWorldPose()
        {
            transform.SetPositionAndRotation(_worldPosition, _worldRotation);
            Vector3 inherited = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(1f / inherited.x, 1f / inherited.y, 1f / inherited.z);
        }

        // 나이트가 점프해도 착지 예고가 기물 발에 붙어 움직이지 않도록 월드 위치를 유지한다.
        // 부모 관계는 유지하므로 소환체를 지울 때 경고 오브젝트도 함께 정리된다.
        private void LateUpdate() { if (_visible) ApplyWorldPose(); }

        public void SetProgress(float progress) { if (_presenter != null) _presenter.SetProgress(progress); }
        public void Hide() { _visible = false; if (_presenter != null) _presenter.SetOpacity(0f); }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }
}
