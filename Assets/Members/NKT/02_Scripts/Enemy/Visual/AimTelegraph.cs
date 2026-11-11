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
    }
}