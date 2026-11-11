using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NKT.Enemy.EditorTools
{
    //URP에서 분홍색으로 나오는 머티리얼(셰이더가 없거나 URP가 그릴 수 없는 셰이더)을 URP Lit으로 바꾼다.
    //메뉴: Tools/NKT/Fix Pink Materials (URP Lit)
    //Project 창에서 폴더나 머티리얼을 선택하고 실행. 아무것도 선택 안 하면 DefaultFolder를 처리한다.
    //원래 셰이더의 프로퍼티 이름이 제각각(Standard, HDRP, 커스텀)이라 저장된 값을 직접 읽어서 옮긴다.
    public static class PinkMaterialFixer
    {
        private const string DefaultFolder = "Assets/Serah Fei Bikini";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        private static readonly string[] BaseMapNames = { "_BaseMap", "_BaseColorMap", "_MainTex", "_UnlitColorMap" };
        private static readonly string[] NormalMapNames = { "_BumpMap", "_NormalMap" };
        private static readonly string[] BaseColorNames = { "_BaseColor", "_Color" };
        private static readonly string[] SmoothnessNames = { "_Smoothness", "_Glossiness" };

        [MenuItem("Tools/NKT/Fix Pink Materials (URP Lit)")]
        private static void FixPinkMaterials()
        {
            Shader urpLit = Shader.Find(UrpLitShaderName);
            if (urpLit == null)
            {
                Debug.LogError($"{UrpLitShaderName} 셰이더를 찾지 못했습니다. URP 패키지를 확인하세요.");
                return;
            }

            List<string> converted = new List<string>();
            foreach (string path in CollectMaterialPaths())
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !IsBroken(material)) continue;

                Convert(material, urpLit);
                converted.Add(path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(converted.Count == 0
                ? "분홍색(깨진) 머티리얼이 없습니다."
                : $"URP Lit으로 변환한 머티리얼 {converted.Count}개:\n{string.Join("\n", converted)}");
        }

        private static IEnumerable<string> CollectMaterialPaths()
        {
            List<string> folders = new List<string>();
            HashSet<string> paths = new HashSet<string>();

            foreach (Object selected in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (AssetDatabase.IsValidFolder(path)) folders.Add(path);
                else if (selected is Material) paths.Add(path);
            }

            if (folders.Count == 0 && paths.Count == 0)
                folders.Add(DefaultFolder);

            if (folders.Count > 0)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", folders.ToArray()))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            return paths;
        }

        //셰이더 파일이 없으면 InternalErrorShader로 바뀌고, Built-in/HDRP 셰이더는 URP가 그리지 못한다.
        //URP용 셰이더(Lit, Shader Graph, 직접 짠 URP 셰이더)는 SubShader에 RenderPipeline=UniversalPipeline 태그가 있다.
        private static bool IsBroken(Material material)
        {
            Shader shader = material.shader;
            if (shader == null || !shader.isSupported) return true;
            if (shader.name == "Hidden/InternalErrorShader") return true;

            string tag = material.GetTag("RenderPipeline", false, string.Empty);
            if (tag == "UniversalPipeline") return false;
            if (tag == "HDRenderPipeline") return true;

            //태그가 없어도 URP에서 그려지는 범용 셰이더들은 건드리지 않는다.
            string name = shader.name;
            return !(name.StartsWith("Sprites/") || name.StartsWith("UI/") || name.StartsWith("TextMeshPro")
                     || name.StartsWith("Skybox/") || name.StartsWith("Hidden/"));
        }

        private static void Convert(Material material, Shader urpLit)
        {
            //셰이더를 바꾸기 전에 저장된 값을 읽어둔다. 셰이더가 없으면 material.GetTexture로는 못 읽는다.
            SerializedObject so = new SerializedObject(material);
            Texture baseMap = FindTexture(so, BaseMapNames);
            Texture normalMap = FindTexture(so, NormalMapNames);
            Color baseColor = FindColor(so, BaseColorNames, Color.white);
            float smoothness = FindFloat(so, SmoothnessNames, 0.3f);
            float cutoff = FindFloat(so, new[] { "_Cutoff", "_AlphaCutoff" }, 0.5f);
            bool alphaClip = material.IsKeywordEnabled("_ALPHATEST_ON")
                             || HasKeyword(so, "_ALPHATEST_ON")
                             || FindFloat(so, new[] { "_AlphaCutoffEnable" }, 0f) > 0.5f
                             || Mathf.Approximately(FindFloat(so, new[] { "_Mode" }, 0f), 1f);
            bool doubleSided = FindFloat(so, new[] { "_DoubleSidedEnable" }, 0f) > 0.5f;

            Undo.RecordObject(material, "Fix Pink Material");
            material.shader = urpLit;
            material.shaderKeywords = new string[0];

            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_MainTex", baseMap);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetFloat("_Metallic", 0f); //캐릭터 피부/머리카락은 금속이 아니다.
            material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));

            material.SetTexture("_BumpMap", normalMap);
            if (normalMap != null) material.EnableKeyword("_NORMALMAP");

            //불투명으로 고정. 원래 셰이더가 남긴 블렌드 값이 섞여 들어가지 않게 URP 기본값으로 덮는다.
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_Cutoff", cutoff);
            material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1; //셰이더 기본값
            }

            material.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            material.doubleSidedGI = doubleSided;

            EditorUtility.SetDirty(material);
        }

        private static SerializedProperty FindSaved(SerializedObject so, string listName, string propertyName)
        {
            SerializedProperty list = so.FindProperty($"m_SavedProperties.{listName}");
            if (list == null) return null;

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == propertyName)
                    return entry.FindPropertyRelative("second");
            }
            return null;
        }

        private static Texture FindTexture(SerializedObject so, string[] names)
        {
            foreach (string name in names)
            {
                SerializedProperty value = FindSaved(so, "m_TexEnvs", name);
                Texture texture = value?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
                if (texture != null) return texture;
            }
            return null;
        }

        private static Color FindColor(SerializedObject so, string[] names, Color fallback)
        {
            foreach (string name in names)
            {
                SerializedProperty value = FindSaved(so, "m_Colors", name);
                if (value != null) return value.colorValue;
            }
            return fallback;
        }

        private static float FindFloat(SerializedObject so, string[] names, float fallback)
        {
            foreach (string name in names)
            {
                SerializedProperty value = FindSaved(so, "m_Floats", name);
                if (value != null) return value.floatValue;
            }
            return fallback;
        }

        //셰이더가 없으면 IsKeywordEnabled가 false를 줄 수 있어서 저장된 키워드 문자열도 확인한다.
        private static bool HasKeyword(SerializedObject so, string keyword)
        {
            foreach (string field in new[] { "m_ShaderKeywords", "m_InvalidKeywords", "m_ValidKeywords" })
            {
                SerializedProperty property = so.FindProperty(field);
                if (property == null) continue;

                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    for (int i = 0; i < property.arraySize; i++)
                        if (property.GetArrayElementAtIndex(i).stringValue == keyword) return true;
                }
                else if (property.propertyType == SerializedPropertyType.String
                         && property.stringValue.Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
