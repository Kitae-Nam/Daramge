using System;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.Visual
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AimTelegraph : MonoBehaviour
    {
        [SerializeField] private int segments = 24;
        [SerializeField] private LineRenderer centerLine;
        [SerializeField] private Color coneColor = Color.red;
        [SerializeField] private Color lineColor = Color.red;
        
        private Mesh _mesh;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _block;
        private Vector3[] _vertices;
        private int[] _triangles;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        
        private void Awake()
        {
            _mesh = new Mesh { name = "AimCone" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _meshRenderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            
            _vertices = new Vector3[segments + 2];
            _triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                _triangles[i * 3] = 0;
                _triangles[i * 3 + 1] = i + 1;
                _triangles[i * 3 + 2] = i + 2;
            }
        }

        public void Show()
        {
            _meshRenderer.enabled = true;
            centerLine.enabled = true;
        }

        public void Hide()
        {
            _meshRenderer.enabled = false;
            centerLine.enabled = false;
        }

        public void ShapeUpdate(float range, float angle, float progress)
        {
            _vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, i / (float)segments);
                _vertices[1+i] = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            }
            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();

            Color cone = coneColor;
            cone.a = 1f - progress;
            _block.SetColor(BaseColorId, cone);
            _meshRenderer.SetPropertyBlock(_block);
            
            Color line = lineColor;
            line.a *= progress;
            centerLine.startColor = centerLine.endColor = line;
            centerLine.SetPosition(0, Vector3.zero);
            centerLine.SetPosition(1, Vector3.forward * range);
        }
    }
}