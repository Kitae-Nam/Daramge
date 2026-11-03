using System.IO;
using CHG._02_Scripts.Agent;
using NKT.Enemy.Module;
using NKT.Enemy.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace NKT.Enemy.EditorTools
{
    //비행 적 베이스 생성. 메뉴: Tools/NKT/Create Flying Enemy Base
    //1. 레이어(FlyingEnemy, FlyNav)와 충돌 설정
    //2. NavMesh Agent Type(Flyer) 추가
    //3. 그림자 텍스처/머티리얼 생성
    //4. 근접 적 프리팹을 복사해서 비행 적 프리팹으로 변환
    public static class FlyingEnemySetup
    {
        private const string SourcePrefabPath = "Assets/Members/NKT/03_GameModules/Enemy/Prefabs/CommonMelee Enemy.prefab";
        private const string TargetPrefabPath = "Assets/Members/NKT/03_GameModules/Enemy/Prefabs/CommonFly Enemy.prefab";
        private const string ShadowFolder = "Assets/Members/NKT/03_GameModules/Enemy/Shadow";
        private const string ShadowTexturePath = ShadowFolder + "/EnemyShadow.png";
        private const string ShadowMaterialPath = ShadowFolder + "/EnemyShadow.mat";

        private const string FlyingLayerName = "FlyingEnemy";
        private const string FlyNavLayerName = "FlyNav";
        private const string PlayerLayerName = "Player";
        private const string FlyerAgentName = "Flyer";

        private const float HoverHeight = 2f;
        private const float FlyerStepHeight = 2f;   //낮은 턱, 장애물을 넘는다.
        private const float FlyerMaxSlope = 60f;

        [MenuItem("Tools/NKT/Create Flying Enemy Base")]
        private static void Create()
        {
            int flyingLayer = EnsureLayer(FlyingLayerName);
            int flyNavLayer = EnsureLayer(FlyNavLayerName);
            if (flyingLayer < 0 || flyNavLayer < 0) return;

            SetupPhysics(flyingLayer, flyNavLayer);
            bool hasFlyerAgent = EnsureAgentType(FlyerAgentName, out int flyerAgentId);
            Material shadowMaterial = GetOrCreateShadowMaterial();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Flying Enemy Base",
                    $"{TargetPrefabPath}가 이미 있습니다. 근접 적 프리팹을 다시 복사해서 덮어쓸까요?", "덮어쓰기", "취소");
                if (!overwrite) return;
                AssetDatabase.DeleteAsset(TargetPrefabPath);
            }

            if (!AssetDatabase.CopyAsset(SourcePrefabPath, TargetPrefabPath))
            {
                Debug.LogError($"[FlyingSetup] 프리팹 복사 실패: {SourcePrefabPath}");
                return;
            }

            BuildPrefab(flyingLayer, hasFlyerAgent, flyerAgentId, shadowMaterial);
            AssetDatabase.SaveAssets();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
            Debug.Log($"[FlyingSetup] 완료: {TargetPrefabPath}");
        }

        #region Project settings

        private static int EnsureLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    return i;
            }

            for (int i = 8; i < layers.arraySize; i++) //0~7은 유니티 기본 레이어
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;

                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[FlyingSetup] 레이어 추가: {layerName} ({i})");
                return i;
            }

            Debug.LogError($"[FlyingSetup] 빈 레이어가 없어 {layerName}을 추가하지 못했습니다.");
            return -1;
        }

        //비행 적은 플레이어 이동을 막지 않고, FlyNav(비행용 바닥)는 NavMesh 굽기 전용이라 아무것과도 충돌하지 않는다.
        private static void SetupPhysics(int flyingLayer, int flyNavLayer)
        {
            int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            if (playerLayer >= 0)
                Physics.IgnoreLayerCollision(flyingLayer, playerLayer, true);

            for (int i = 0; i < 32; i++)
                Physics.IgnoreLayerCollision(flyNavLayer, i, true);

            Debug.Log("[FlyingSetup] 충돌 설정: FlyingEnemy-Player 무시, FlyNav-전체 무시");
        }

        private static bool EnsureAgentType(string agentName, out int agentTypeId)
        {
            agentTypeId = 0;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset");
            if (assets.Length == 0)
            {
                Debug.LogWarning("[FlyingSetup] NavMesh 설정을 찾을 수 없습니다. Flyer Agent Type은 직접 추가하세요.");
                return false;
            }

            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty names = so.FindProperty("m_SettingNames");
            SerializedProperty settings = so.FindProperty("m_Settings");
            if (names == null || settings == null || settings.arraySize == 0)
            {
                Debug.LogWarning("[FlyingSetup] NavMesh Agent 설정 형식이 달라 자동 추가하지 못했습니다. Flyer Agent Type은 직접 추가하세요.");
                return false;
            }

            for (int i = 0; i < names.arraySize; i++)
            {
                if (names.GetArrayElementAtIndex(i).stringValue != agentName) continue;
                agentTypeId = settings.GetArrayElementAtIndex(i).FindPropertyRelative("agentTypeID").intValue;
                return true;
            }

            //마지막 Agent 설정을 복사해서 추가
            settings.InsertArrayElementAtIndex(settings.arraySize - 1);
            names.InsertArrayElementAtIndex(names.arraySize - 1);
            SerializedProperty newSetting = settings.GetArrayElementAtIndex(settings.arraySize - 1);
            names.GetArrayElementAtIndex(names.arraySize - 1).stringValue = agentName;

            agentTypeId = CreateUniqueAgentTypeId(settings);
            newSetting.FindPropertyRelative("agentTypeID").intValue = agentTypeId;
            newSetting.FindPropertyRelative("agentClimb").floatValue = FlyerStepHeight;
            newSetting.FindPropertyRelative("agentSlope").floatValue = FlyerMaxSlope;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[FlyingSetup] NavMesh Agent Type 추가: {agentName} (Step Height {FlyerStepHeight}, Max Slope {FlyerMaxSlope})");
            return true;
        }

        private static int CreateUniqueAgentTypeId(SerializedProperty settings)
        {
            while (true)
            {
                int id = Random.Range(1, int.MaxValue);
                bool isUsed = false;
                for (int i = 0; i < settings.arraySize - 1; i++)
                {
                    if (settings.GetArrayElementAtIndex(i).FindPropertyRelative("agentTypeID").intValue == id)
                        isUsed = true;
                }
                if (!isUsed) return id;
            }
        }

        #endregion

        #region Shadow

        private static Material GetOrCreateShadowMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
            if (material != null) return material;

            if (!AssetDatabase.IsValidFolder(ShadowFolder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(ShadowFolder)?.Replace('\\', '/'), Path.GetFileName(ShadowFolder));

            Texture2D texture = CreateShadowTexture();

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else material.mainTexture = texture;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.5f));

            //URP Unlit을 반투명으로
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            AssetDatabase.CreateAsset(material, ShadowMaterialPath);
            return material;
        }

        private static Texture2D CreateShadowTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Sqrt(dx * dx + dy * dy)); //가운데 진하고 가장자리 흐린 원
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            File.WriteAllBytes(ShadowTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ShadowTexturePath);

            if (AssetImporter.GetAtPath(ShadowTexturePath) is TextureImporter importer)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexturePath);
        }

        #endregion

        #region Prefab

        private static void BuildPrefab(int flyingLayer, bool hasFlyerAgent, int flyerAgentId, Material shadowMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TargetPrefabPath);
            try
            {
                root.name = "CommonFly";

                SwapEnemyClass(root);
                SwapRenderer(root);
                RemoveMeleeSkills(root);
                SetupHurtbox(root, flyingLayer);
                Transform shadow = CreateShadow(root, shadowMaterial);
                SetupFlyHeight(root, shadow);
                SetupNavAgent(root, hasFlyerAgent, flyerAgentId);
                SetupSensor(root);

                PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SwapEnemyClass(GameObject root)
        {
            EnemyDataSO enemyData = null;
            CommonMeleeEnemy melee = root.GetComponent<CommonMeleeEnemy>();
            if (melee != null)
            {
                enemyData = melee.EnemyData;
                Object.DestroyImmediate(melee);
            }

            FlyingEnemy flying = root.GetComponent<FlyingEnemy>();
            if (flying == null)
                flying = root.AddComponent<FlyingEnemy>();

            SerializedObject so = new SerializedObject(flying);
            so.FindProperty("<EnemyData>k__BackingField").objectReferenceValue = enemyData;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        //비행 애니메이션은 제자리 클립이라 루트모션 렌더러 대신 기본 렌더러를 쓴다.
        private static void SwapRenderer(GameObject root)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning("[FlyingSetup] Animator가 없어 렌더러를 바꾸지 못했습니다.");
                return;
            }

            EnemyNavRenderer navRenderer = animator.GetComponent<EnemyNavRenderer>();
            if (navRenderer != null)
                Object.DestroyImmediate(navRenderer);

            bool hasAgentRenderer = false;
            foreach (AgentRenderer agentRenderer in animator.GetComponents<AgentRenderer>())
                hasAgentRenderer |= agentRenderer.GetType() == typeof(AgentRenderer);
            if (!hasAgentRenderer)
                animator.gameObject.AddComponent<AgentRenderer>();

            animator.applyRootMotion = false;
        }

        private static void RemoveMeleeSkills(GameObject root)
        {
            foreach (CommonMeleeSkill skill in root.GetComponentsInChildren<CommonMeleeSkill>(true))
            {
                try
                {
                    Object.DestroyImmediate(skill.gameObject);
                }
                catch (System.Exception)
                {
                    Debug.LogWarning($"[FlyingSetup] {skill.gameObject.name}의 근접 스킬은 중첩 프리팹 안에 있어 자동으로 지우지 못했습니다. 직접 제거하세요.");
                }
            }
        }

        //플레이어 공격은 IDamageable이 붙은 오브젝트의 콜라이더를 찾으므로 루트에 둔다. 레이어는 FlyHeightModule이 높이에 따라 바꾼다.
        private static void SetupHurtbox(GameObject root, int flyingLayer)
        {
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = root.AddComponent<CapsuleCollider>();

            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 1.6f;
            capsule.radius = 0.4f;
            root.layer = flyingLayer;
        }

        private static Transform CreateShadow(GameObject root, Material shadowMaterial)
        {
            Transform existing = root.transform.Find("Shadow");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "Shadow";
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, -HoverHeight, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = Vector3.one * 1.2f;

            MeshRenderer meshRenderer = shadow.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = shadowMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return shadow.transform;
        }

        private static void SetupFlyHeight(GameObject root, Transform shadow)
        {
            FlyHeightModule flyHeight = root.GetComponent<FlyHeightModule>();
            if (flyHeight == null)
                flyHeight = root.AddComponent<FlyHeightModule>();

            SerializedObject so = new SerializedObject(flyHeight);
            so.FindProperty("hoverHeight").floatValue = HoverHeight;
            so.FindProperty("shadow").objectReferenceValue = shadow;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupNavAgent(GameObject root, bool hasFlyerAgent, int flyerAgentId)
        {
            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent == null) return;

            agent.baseOffset = HoverHeight;
            if (hasFlyerAgent)
                agent.agentTypeID = flyerAgentId;
            else
                Debug.LogWarning("[FlyingSetup] Flyer Agent Type이 없어 Agent Type을 바꾸지 못했습니다. 프리팹에서 직접 설정하세요.");
        }

        //루트가 공중에 떠 있으므로 시야 레이를 더 위에서 쏘지 않게 한다.
        private static void SetupSensor(GameObject root)
        {
            AgentSensor sensor = root.GetComponentInChildren<AgentSensor>(true);
            if (sensor == null) return;

            SerializedObject so = new SerializedObject(sensor);
            SerializedProperty eyeHeight = so.FindProperty("eyeHeight");
            if (eyeHeight != null)
            {
                eyeHeight.floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        #endregion
    }
}
