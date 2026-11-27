using UnityEngine;
using UnityEngine.Rendering;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>쓸기 방향으로 바닥 자국을 드러내고 시간이 지나면 지운다. 피해 판정은 소유하지 않는다.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ChessSweepGroundMark : MonoBehaviour
    {
        private const int Segments = 30;
        private const int Strips = 3;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _properties;
        private Color _color;
        private int[] _indices;
        private int _shown;
        private float _remaining;
        private float _lifetime;
        private bool _released;

        public void Initialize(Material material, float width, float depth, Color color, float lifetime)
        {
            _lifetime = Mathf.Max(.01f, lifetime);
            _remaining = _lifetime;
            _color = color;
            _renderer = GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _properties = new MaterialPropertyBlock();
            SetOpacity(1f);

            int stripVertices = (Segments + 1) * 2;
            var vertices = new Vector3[Strips * stripVertices];
            var uv = new Vector2[vertices.Length];
            _indices = new int[Segments * Strips * 6];
            for (int i = 0; i <= Segments; i++)
            {
                float progress = i / (float)Segments;
                float x = Mathf.Lerp(width * .48f, -width * .48f, progress);
                for (int strip = 0; strip < Strips; strip++)
                {
                    // 안쪽에 좁은 흠집을 겹쳐 놓고 양 끝을 가늘게 만든다.
                    float center = depth * ((strip - 1) * .19f + .12f * Mathf.Sin(progress * Mathf.PI)) + .025f * Mathf.Sin(i * .81f + strip);
                    float half = depth * (strip == 1 ? .055f : .025f) * Mathf.Lerp(.12f, 1f, Mathf.Sin(progress * Mathf.PI));
                    int v = strip * stripVertices + i * 2;
                    vertices[v] = new Vector3(x, 0, center - half);
                    vertices[v + 1] = new Vector3(x, 0, center + half);
                    uv[v] = new Vector2(0f, progress);
                    uv[v + 1] = new Vector2(1f, progress);
                    if (i == Segments) continue;
                    int t = (i * Strips + strip) * 6;
                    _indices[t] = v;
                    _indices[t + 1] = v + 1;
                    _indices[t + 2] = v + 2;
                    _indices[t + 3] = v + 2;
                    _indices[t + 4] = v + 1;
                    _indices[t + 5] = v + 3;
                }
            }

            _mesh = new Mesh { name = "Colossus sweep scratches" };
            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        public void Reveal(float progress)
        {
            int visible = Mathf.Clamp(Mathf.CeilToInt(progress * Segments), _shown, Segments);
            if (visible == _shown) return;
            _shown = visible;
            _mesh.SetTriangles(_indices, 0, visible * Strips * 6, 0, false);
        }

        public void Release() => _released = true;

        private void Update()
        {
            if (!_released) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) { Destroy(gameObject); return; }
            SetOpacity(Mathf.Clamp01(_remaining / Mathf.Min(.65f, _lifetime)));
        }

        private void SetOpacity(float fraction)
        {
            Color color = _color;
            color.a *= fraction;
            _properties.SetColor(BaseColor, color);
            _renderer.SetPropertyBlock(_properties);
        }

        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }
}
