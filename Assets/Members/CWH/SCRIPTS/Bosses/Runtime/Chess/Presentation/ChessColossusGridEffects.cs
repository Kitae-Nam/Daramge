using System.Collections;
using System.Collections.Generic;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Presentation
{
    /// <summary>판정과 같은 월드 칸을 예고하고, 타격 시 칸이나 기물을 표시한다.</summary>
    public sealed class ChessColossusGridEffects : MonoBehaviour
    {
        [SerializeField] private Material _tileMaterial;
        [SerializeField] private Material _warningMaterial;
        public enum Piece { None, Rook, Bishop, Knight }

        [SerializeField] private GameObject _rook;
        [SerializeField, Min(.1f), Tooltip("돌진하는 룩의 높이를 체스판 한 칸 너비에 대한 배율로 지정합니다. 1은 100%입니다.")]
        private float _chargingRookHeightRatio = .82f;
        [SerializeField] private GameObject _rookChargeDust;
        [SerializeField] private GameObject _rookCrashSparks;
        [SerializeField] private GameObject _bishop;
        [SerializeField] private GameObject _knight;
        [SerializeField, Min(.1f), Tooltip("점프 후 솟는 기물의 높이를 체스판 한 칸 너비에 대한 배율로 지정합니다. 1은 100%입니다.")]
        private float _jumpPieceHeightRatio = 1f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private readonly List<GameObject> _active = new List<GameObject>();
        private readonly Dictionary<GameObject, Mesh> _meshes = new Dictionary<GameObject, Mesh>();
        private readonly List<GameObject> _chargingRooks = new List<GameObject>(2);
        private readonly List<GameObject> _rookParticles = new List<GameObject>();
        private readonly float[] _nextDustDistance = new float[2];
        private float _rookSurfaceHeight;
        private float _rookCellSize;
        private float _rookHiddenDepth;
        private BossTelegraphPresenter _primary;
        private BossTelegraphPresenter _secondary;

        public void Show(IReadOnlyList<Vector3> primary, IReadOnlyList<Vector3> secondary, float size, float surfaceHeight)
        {
            HideWarnings();
            _primary = Warning(primary, size, surfaceHeight + .004f, "Colossus main cells", .94f);
            _secondary = Warning(secondary, size, surfaceHeight + .002f, "Colossus follow-up cells", .78f);
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (_primary != null) _primary.SetProgress(progress);
            if (_secondary != null) _secondary.SetProgress(progress);
        }

        public void HideWarnings()
        {
            HidePrimary();
            if (_secondary != null) Remove(_secondary.gameObject);
            _secondary = null;
        }

        public void HidePrimary()
        {
            if (_primary != null) Remove(_primary.gameObject);
            _primary = null;
        }

        public void Strike(IReadOnlyList<Vector3> cells, float size, Piece pieceKind, float surfaceHeight)
        {
            if (pieceKind == Piece.None)
            {
                var flash = Tiles(cells, size, surfaceHeight + .006f, "Colossus grid impact", _tileMaterial);
                if (flash != null) StartCoroutine(Flash(flash));
                return;
            }
            GameObject prefab = pieceKind switch { Piece.Rook => _rook, Piece.Bishop => _bishop, _ => _knight };
            if (prefab == null) return;
            float targetHeight = size * Mathf.Max(.1f, _jumpPieceHeightRatio);
            foreach (Vector3 cell in cells)
            {
                var surface = new Vector3(cell.x, surfaceHeight, cell.z);
                GameObject piece = SpawnPiece(prefab, surface, targetHeight);
                StartCoroutine(Rise(piece, surface, size, targetHeight));
            }
        }

        public void BeginRookCharge(IReadOnlyList<Vector3> starts, float size, float surfaceHeight)
        {
            CancelRookCharge();
            if (_rook == null) return;
            _rookSurfaceHeight = surfaceHeight;
            _rookCellSize = size;
            float rookHeight = size * Mathf.Max(.1f, _chargingRookHeightRatio);
            // 크기를 키워도 등장 전에는 룩 전체가 체스판 아래에 있도록 깊이를 함께 계산한다.
            _rookHiddenDepth = rookHeight + size * .04f;
            foreach (Vector3 cell in starts)
            {
                var point = new Vector3(cell.x, surfaceHeight - _rookHiddenDepth, cell.z);
                GameObject rook = SpawnPiece(_rook, point, rookHeight);
                rook.name = "Colossus charging rook";
                _nextDustDistance[_chargingRooks.Count] = size * .2f;
                _chargingRooks.Add(rook);
            }
        }

        public void MoveChargingRook(int lane, Vector3 start, Vector3 offset, float riseProgress)
        {
            if (lane >= _chargingRooks.Count || _chargingRooks[lane] == null) return;
            Vector3 position = start + offset;
            position.y = _rookSurfaceHeight + Mathf.SmoothStep(-_rookHiddenDepth, .015f, riseProgress);
            _chargingRooks[lane].transform.position = position;
            TrailRookDust(lane, start, offset);
        }

        private void TrailRookDust(int lane, Vector3 start, Vector3 offset)
        {
            float distance = offset.magnitude;
            if (_rookChargeDust == null || distance < _nextDustDistance[lane]) return;
            Vector3 direction = offset / distance;

            // 룩별 이동 거리를 기준으로 먼지를 뿌려, 벽에 막힌 열의 흔적은 그 자리에서 끊긴다.
            while (_nextDustDistance[lane] <= distance)
            {
                Vector3 point = start + direction * (_nextDustDistance[lane] - _rookCellSize * .18f);
                point.y = _rookSurfaceHeight + .025f;
                SpawnRookParticles(_rookChargeDust, point, Quaternion.identity, .27f, "Colossus rook charge dust");
                _nextDustDistance[lane] += _rookCellSize * .65f;
            }
        }

        public void CrashChargingRook(int lane, Vector3 contact, Vector3 normal, Vector3 forward)
        {
            if (lane >= _chargingRooks.Count || _chargingRooks[lane] == null) return;
            Remove(_chargingRooks[lane]);
            _chargingRooks[lane] = null;

            // 불꽃은 접점에서, 먼지는 살짝 뒤쪽 바닥에서 터져 돌진이 급하게 멈추는 느낌을 준다.
            Vector3 sparks = contact + normal * .02f;
            SpawnRookParticles(_rookCrashSparks, sparks, Quaternion.LookRotation(normal), .52f, "Colossus rook crash sparks");
            Vector3 dust = contact - forward * (_rookCellSize * .22f);
            dust.y = _rookSurfaceHeight + .025f;
            SpawnRookParticles(_rookChargeDust, dust, Quaternion.identity, .43f, "Colossus rook crash dust");
        }

        private void SpawnRookParticles(GameObject prefab, Vector3 point, Quaternion direction, float scale, string label)
        {
            if (prefab == null) return;
            var particles = Instantiate(prefab, point, direction * prefab.transform.rotation);
            particles.name = label;
            particles.transform.localScale *= _rookCellSize * scale;
            _rookParticles.Add(particles);
            Destroy(particles, .7f);
        }

        public void CancelRookCharge()
        {
            foreach (GameObject rook in _chargingRooks)
                if (rook != null) Remove(rook);
            _chargingRooks.Clear();
            foreach (GameObject particles in _rookParticles)
            {
                if (particles == null) continue;
                particles.SetActive(false);
                Destroy(particles);
            }
            _rookParticles.Clear();
        }

        private GameObject SpawnPiece(GameObject prefab, Vector3 position, float targetHeight)
        {
            GameObject piece = Instantiate(prefab, position, Quaternion.identity);
            _active.Add(piece);
            // 피해는 공격 타이머가 판정하므로 표시용 룩이 플레이어 이동을 막지 않게 한다.
            foreach (var collider in piece.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var renderer = piece.GetComponentInChildren<MeshRenderer>();
            float height = renderer != null ? renderer.bounds.size.y : 1f;
            piece.transform.localScale *= targetHeight / Mathf.Max(.01f, height);
            return piece;
        }

        private BossTelegraphPresenter Warning(IReadOnlyList<Vector3> cells, float size, float height, string label, float opacity)
        {
            var renderer = Tiles(cells, size, height, label, _warningMaterial != null ? _warningMaterial : _tileMaterial, true);
            if (renderer == null) return null;
            var presenter = renderer.gameObject.AddComponent<BossTelegraphPresenter>();
            presenter.ConfigureRenderer(renderer);
            presenter.SetOpacity(opacity);
            return presenter;
        }

        private MeshRenderer Tiles(IReadOnlyList<Vector3> cells, float size, float displayHeight, string label, Material material, bool warning = false)
        {
            if (material == null || cells.Count == 0) return null;
            var root = new GameObject(label);
            _active.Add(root);
            var mesh = new Mesh { name = label };
            _meshes.Add(root, mesh);
            var vertices = new Vector3[cells.Count * 4];
            var uv = new List<Vector3>(vertices.Length);
            var edges = warning ? new List<Vector4>(vertices.Length) : null;
            var occupied = warning ? new HashSet<Vector2Int>() : null;
            Vector3 anchor = cells[0];
            float minZ = anchor.z;
            float maxZ = anchor.z;
            if (occupied != null)
            {
                foreach (Vector3 cell in cells)
                {
                    occupied.Add(GridIndex(cell, anchor, size));
                    minZ = Mathf.Min(minZ, cell.z);
                    maxZ = Mathf.Max(maxZ, cell.z);
                }
            }
            var triangles = new int[cells.Count * 6];
            // 판정 칸의 전체 폭을 사용해 인접한 경고 표시가 같은 경계에서 맞닿게 한다.
            float half = size * .5f;
            for (int i = 0; i < cells.Count; i++)
            {
                int v = i * 4, t = i * 6;
                // 색을 깔 바닥 높이는 거신병/NavMesh 높이와 독립적이다. 작은 간격은 바닥과 겹쳐 깜빡이는 현상만 막는다.
                Vector3 p = cells[i];
                p.y = displayHeight;
                vertices[v] = p + new Vector3(-half, 0, -half);
                vertices[v + 1] = p + new Vector3(-half, 0, half);
                vertices[v + 2] = p + new Vector3(half, 0, half);
                vertices[v + 3] = p + new Vector3(half, 0, -half);
                if (edges != null)
                {
                    // 칸별 UV의 XY는 외곽선에, 경고 전체에서 공유하는 Z는 끊김 없는 채움에 사용한다.
                    float near = Mathf.InverseLerp(minZ - half, maxZ + half, vertices[v].z);
                    float far = Mathf.InverseLerp(minZ - half, maxZ + half, vertices[v + 1].z);
                    uv.Add(new Vector3(0f, 0f, near));
                    uv.Add(new Vector3(0f, 1f, far));
                    uv.Add(new Vector3(1f, 1f, far));
                    uv.Add(new Vector3(1f, 0f, near));
                    Vector2Int at = GridIndex(cells[i], anchor, size);
                    // 붙어 있는 칸의 공통 경계는 선을 그리지 않아 하나의 경고 영역으로 보인다.
                    var exposed = new Vector4(occupied.Contains(at + Vector2Int.left) ? 0f : 1f,
                        occupied.Contains(at + Vector2Int.right) ? 0f : 1f,
                        occupied.Contains(at + Vector2Int.down) ? 0f : 1f,
                        occupied.Contains(at + Vector2Int.up) ? 0f : 1f);
                    for (int j = 0; j < 4; j++) edges.Add(exposed);
                }
                else for (int j = 0; j < 4; j++) uv.Add(new Vector3(.5f, .5f, 0f));
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            mesh.vertices = vertices;
            mesh.SetUVs(0, uv);
            mesh.triangles = triangles;
            if (edges != null) mesh.SetUVs(1, edges);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Vector2Int GridIndex(Vector3 point, Vector3 anchor, float size) =>
            new Vector2Int(Mathf.RoundToInt((point.x - anchor.x) / size), Mathf.RoundToInt((point.z - anchor.z) / size));

        private static void Tint(MeshRenderer renderer, Color color)
        {
            if (renderer == null) return;
            var properties = new MaterialPropertyBlock();
            properties.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(properties);
        }

        private IEnumerator Flash(MeshRenderer flash)
        {
            for (float time = 0; time < .32f; time += Time.deltaTime)
            {
                Tint(flash, new Color(1f, .59f, .14f, .86f * (1f - time / .32f)));
                yield return null;
            }
            Remove(flash.gameObject);
        }

        private IEnumerator Rise(GameObject piece, Vector3 surface, float size, float pieceHeight)
        {
            // 시작과 종료 깊이를 기물 높이에 맞춰 큰 기물도 바닥 아래에서 완전히 숨긴다.
            float hiddenDepth = pieceHeight + size * .04f;
            for (float time = 0; time < .58f; time += Time.deltaTime)
            {
                float height = Mathf.SmoothStep(-hiddenDepth, size * .28f, time / .095f);
                height = Mathf.Lerp(height, -hiddenDepth, Mathf.SmoothStep(0f, 1f, (time - .25f) / .33f));
                piece.transform.position = surface + Vector3.up * height;
                yield return null;
            }
            Remove(piece);
        }

        private void Remove(GameObject item)
        {
            _active.Remove(item);
            if (_meshes.TryGetValue(item, out Mesh mesh))
            {
                _meshes.Remove(item);
                Destroy(mesh);
            }
            item.SetActive(false);
            Destroy(item);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            CancelRookCharge();
            while (_active.Count > 0) Remove(_active[_active.Count - 1]);
            _primary = _secondary = null;
        }
    }
}
