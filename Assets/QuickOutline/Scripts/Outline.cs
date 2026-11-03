//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuickOutline.Scripts
{
  [DisallowMultipleComponent]

  public class Outline : MonoBehaviour {
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode {
      OutlineAll,
      OutlineVisible,
      OutlineHidden,
      OutlineAndSilhouette,
      SilhouetteOnly
    }

    public Mode OutlineMode {
      get { return outlineMode; }
      set {
        outlineMode = value;
        needsUpdate = true;
      }
    }

    public Color OutlineColor {
      get { return outlineColor; }
      set {
        outlineColor = value;
        needsUpdate = true;
      }
    }

    public float OutlineWidth {
      get { return outlineWidth; }
      set {
        outlineWidth = value;
        needsUpdate = true;
      }
    }

    // 런타임 변경도 기존 머티리얼 갱신 경로를 사용해 Inspector 변경과 동일하게 반영한다.
    public bool HideOccluded {
      get { return hideOccluded; }
      set {
        hideOccluded = value;
        needsUpdate = true;
      }
    }

    [Serializable]
    private class ListVector3 {
      public List<Vector3> data;
    }

    [SerializeField]
    private Mode outlineMode;

    [SerializeField]
    private Color outlineColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 2f;

    // 기본값을 false로 두어 기존 씬과 프리팹의 외곽선 표시 방식을 유지한다.
    [SerializeField, Tooltip("켜면 Outline Mode보다 우선하여 보이는 부분의 외곽선만 표시합니다. "
                             + "깊이 버퍼에 기록되는 벽이나 오브젝트에 가려진 부분은 표시하지 않습니다.")]
    private bool hideOccluded;

    [Header("Optional")]

    [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
                             + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
    private bool precomputeOutline;

    [SerializeField, HideInInspector]
    private List<Mesh> bakeKeys = new List<Mesh>();

    [SerializeField, HideInInspector]
    private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;

    private bool needsUpdate;

    void Awake() {

      // Cache renderers
      renderers = GetComponentsInChildren<Renderer>()
        .Where(r => !(r is ParticleSystemRenderer) && !(r is LineRenderer))
        .ToArray();

      // Instantiate outline materials
      outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
      outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

      outlineMaskMaterial.name = "OutlineMask (Instance)";
      outlineFillMaterial.name = "OutlineFill (Instance)";

      // Retrieve or generate smooth normals
      LoadSmoothNormals();

      // Apply material properties immediately
      needsUpdate = true;
    }

    void OnEnable() {
      foreach (var renderer in renderers) {

        // Append outline shaders
        var materials = renderer.sharedMaterials.ToList();

        materials.Add(outlineMaskMaterial);
        materials.Add(outlineFillMaterial);

        renderer.materials = materials.ToArray();
      }
    }

    void OnValidate() {

      // Update material properties
      needsUpdate = true;

      // Clear cache when baking is disabled or corrupted
      if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
        bakeKeys.Clear();
        bakeValues.Clear();
      }

      // Generate smooth normals when baking is enabled
      if (precomputeOutline && bakeKeys.Count == 0) {
        Bake();
      }
    }

    void Update() {
      if (needsUpdate) {
        needsUpdate = false;

        UpdateMaterialProperties();
      }
    }

    void OnDisable() {
      foreach (var renderer in renderers) {

        // Remove outline shaders
        var materials = renderer.sharedMaterials.ToList();

        materials.Remove(outlineMaskMaterial);
        materials.Remove(outlineFillMaterial);

        renderer.materials = materials.ToArray();
      }
    }

    void OnDestroy() {

      // Destroy material instances
      Destroy(outlineMaskMaterial);
      Destroy(outlineFillMaterial);
    }

    void Bake() {

      // Generate smooth normals for each mesh
      var bakedMeshes = new HashSet<Mesh>();

      foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

        // Skip duplicates
        if (!bakedMeshes.Add(meshFilter.sharedMesh)) {
          continue;
        }

        // Serialize smooth normals
        var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

        bakeKeys.Add(meshFilter.sharedMesh);
        bakeValues.Add(new ListVector3() { data = smoothNormals });
      }
    }

    void LoadSmoothNormals() {

      // Retrieve or generate smooth normals
      foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

        // Skip if smooth normals have already been adopted
        if (!registeredMeshes.Add(meshFilter.sharedMesh)) {
          continue;
        }

        // Retrieve or generate smooth normals
        var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
        var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

        // Store smooth normals in UV3
        meshFilter.sharedMesh.SetUVs(3, smoothNormals);

        // Combine submeshes
        var renderer = meshFilter.GetComponent<Renderer>();

        if (renderer != null) {
          CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
        }
      }

      // Clear UV3 on skinned mesh renderers
      foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {

        // Skip if UV3 has already been reset
        if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) {
          continue;
        }

        // Clear UV3
        skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];

        // Combine submeshes
        CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
      }
    }

    List<Vector3> SmoothNormals(Mesh mesh) {

      // Group vertices by location
      var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

      // Copy normals to a new list
      var smoothNormals = new List<Vector3>(mesh.normals);

      // Average normals for grouped vertices
      foreach (var group in groups) {

        // Skip single vertices
        if (group.Count() == 1) {
          continue;
        }

        // Calculate the average normal
        var smoothNormal = Vector3.zero;

        foreach (var pair in group) {
          smoothNormal += smoothNormals[pair.Value];
        }

        smoothNormal.Normalize();

        // Assign smooth normal to each vertex
        foreach (var pair in group) {
          smoothNormals[pair.Value] = smoothNormal;
        }
      }

      return smoothNormals;
    }

    void CombineSubmeshes(Mesh mesh, Material[] materials) {

      // Skip meshes with a single submesh
      if (mesh.subMeshCount == 1) {
        return;
      }

      // Skip if submesh count exceeds material count
      if (mesh.subMeshCount > materials.Length) {
        return;
      }

      // Append combined submesh
      mesh.subMeshCount++;
      mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    void UpdateMaterialProperties() {

      // Apply properties according to mode
      outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

      // 기존 OutlineVisible의 깊이 판정을 재사용해 가려진 픽셀만 제외한다.
      // 저장된 모드는 유지하므로 옵션을 끄면 원래 외곽선 모드로 돌아간다.
      var effectiveMode = hideOccluded ? Mode.OutlineVisible : outlineMode;

      switch (effectiveMode) {
        case Mode.OutlineAll:
          outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
          outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
          outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
          break;

        case Mode.OutlineVisible:
          outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
          outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
          outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
          break;

        case Mode.OutlineHidden:
          outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
          outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
          outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
          break;

        case Mode.OutlineAndSilhouette:
          outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
          outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
          outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
          break;

        case Mode.SilhouetteOnly:
          outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
          outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
          outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
          break;
      }
    }
  }
}
