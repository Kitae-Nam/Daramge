#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using Memebers.CWH.SCRIPTS.TEST;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Members.CWH.SCRIPTS.Editor
{
    /// <summary>
    /// 수동 연결 실수를 줄이는 에디터 조립 도구. 생성 결과는 전부 CWH 안에 저장한다.
    /// 런타임은 AssetDatabase/에디터 메뉴 없이 동작하며 기존 자산을 재생성해 덮어쓰지 않는다.
    /// </summary>
    public static class PhaseOneSetupMenu
    {
        public const string Root = "Assets/Members/CWH/PREFABS/PhaseOne";
        public const string DemoScene = "Assets/Members/CWH/SCENCES/PhaseOne_Playground.unity";
        private const string Chess = "Assets/Chess/LowpolyChessPack/Prefabs/";
        private const string CircleMaterial = "Assets/Members/CWH/SHADERS/Telegraphs/Telegraph Mat.mat";
        private const string DashMaterial = "Assets/Members/CWH/SHADERS/Telegraphs/DashWarning Mat.mat";

        [MenuItem("CWH/Boss/1페이즈/현재 보스 연결 및 저장")]
        public static void SetupSelected()
        {
            if (EditorApplication.isPlaying) return;
            BossTarget boss = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<BossTarget>() : null;
            if (boss == null)
            {
                var bosses = Object.FindObjectsByType<BossTarget>().Where(value => value.gameObject.scene == SceneManager.GetActiveScene()).ToArray();
                if (bosses.Length == 1) boss = bosses[0];
            }
            if (boss == null) { Debug.LogError("BossTarget이 있는 보스 루트를 선택하세요."); return; }
            EnsureAssets();
            ConfigureBoss(boss.gameObject, boss.Target);
            if (boss.HasTarget && boss.Target.GetComponent<TestCameraController>() != null)
            {
                var controls = Add<PhaseOneDemoControls>(boss.Target.gameObject);
                SetObject(controls, "_camera", Camera.main);
                SetObject(controls, "_boss", boss.GetComponent<BossHealth>());
                SetObject(controls, "_brain", boss.GetComponent<PhaseOneBrain>());
                // 기존 R/T/Y 테스트와 새 조작이 겹치지 않게 같은 씬의 이전 테스트 입력만 끈다.
                foreach (TestTakeAll previous in Object.FindObjectsByType<TestTakeAll>())
                    if (previous.gameObject.scene == boss.gameObject.scene) previous.enabled = false;
            }
            EditorSceneManager.MarkSceneDirty(boss.gameObject.scene);
            EditorSceneManager.SaveScene(boss.gameObject.scene);
            Debug.Log("1페이즈 연결 완료. Phase One Brain을 끄면 각 패턴을 수동 테스트할 수 있습니다.", boss);
        }

        [MenuItem("CWH/Boss/1페이즈/테스트 씬 열기")]
        public static void OpenDemo()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScene) == null) BuildDemoBatch();
            else EditorSceneManager.OpenScene(DemoScene);
        }

        // 별도 검증 프로젝트에서도 같은 조립 코드를 실행해 저장 결과를 검증한다.
        public static void BuildDemoBatch()
        {
            EnsureAssets();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material floor = EnsureMaterial("ArenaFloor", new Color(0.12f, 0.16f, 0.21f));
            Material wall = EnsureMaterial("ArenaWall", new Color(0.32f, 0.37f, 0.45f));
            MakeCube("Arena floor", new Vector3(0f, -0.5f, 0f), new Vector3(48f, 1f, 48f), floor);
            MakeCube("North wall", new Vector3(0f, 2f, 24f), new Vector3(50f, 4f, 1f), wall);
            MakeCube("South wall", new Vector3(0f, 2f, -24f), new Vector3(50f, 4f, 1f), wall);
            MakeCube("East wall", new Vector3(24f, 2f, 0f), new Vector3(1f, 4f, 48f), wall);
            MakeCube("West wall", new Vector3(-24f, 2f, 0f), new Vector3(1f, 4f, 48f), wall);
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = 1.6f;
            settings.agentHeight = 6.5f;
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.5f, 0f), Quaternion.identity, Vector3.one), size = new Vector3(46f, 1f, 46f), area = 0 }
            };
            NavMeshData nav = AssetDatabase.LoadAssetAtPath<NavMeshData>(Root + "/ArenaNavMesh.asset");
            if (nav == null)
            {
                nav = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(60f, 20f, 60f)), Vector3.zero, Quaternion.identity);
                AssetDatabase.CreateAsset(nav, Root + "/ArenaNavMesh.asset");
            }
            var navigation = new GameObject("Arena navigation").AddComponent<BossArenaNavMesh>();
            SetObject(navigation, "_data", nav);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.7f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.65f, 0.68f, 0.75f);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 38f, -34f);
            camera.transform.rotation = Quaternion.LookRotation(Vector3.zero - camera.transform.position);
            camera.fieldOfView = 58f;
            camera.farClipPlane = 150f;
            camera.gameObject.AddComponent<AudioListener>();

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PhaseOne test player";
            player.transform.position = new Vector3(0f, 1f, -10f);
            player.GetComponent<Renderer>().sharedMaterial = EnsureMaterial("PlayerBlue", new Color(0.1f, 0.65f, 1f));
            var body = player.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var movement = player.AddComponent<TestCameraController>();
            SetObject(movement, "cameraTransform", camera.transform);
            SetFloat(movement, "moveSpeed", 10f);
            player.AddComponent<TestDamageReceiver>();

            GameObject boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PhaseOneBoss.prefab"));
            boss.name = "White King - Phase One";
            boss.transform.position = new Vector3(0f, 0f, 8f);
            SetObject(boss.GetComponent<BossTarget>(), "_target", player.transform);
            var controls = player.AddComponent<PhaseOneDemoControls>();
            SetObject(controls, "_camera", camera);
            SetObject(controls, "_boss", boss.GetComponent<BossHealth>());
            SetObject(controls, "_brain", boss.GetComponent<PhaseOneBrain>());
            EditorSceneManager.SaveScene(scene, DemoScene);
            AssetDatabase.SaveAssets();
            Debug.Log("1페이즈 테스트 씬 저장 완료: " + DemoScene);
        }

        public static void EnsureAssets()
        {
            EnsureFolder(Root);
            Material ivory = EnsureMaterial("WhiteChess", new Color(0.91f, 0.89f, 0.78f));
            CreateMinion("PhaseOnePawn", "White Pawn", ChessMinionKind.Pawn, 2.7f, ivory);
            CreateMinion("PhaseOneKnight", "White Knight", ChessMinionKind.Knight, 3.3f, ivory);
            CreateSword();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PhaseOneBoss.prefab") != null) return;
            var boss = new GameObject("PhaseOneBoss");
            GameObject visual = CreateVisual(Chess + "White King.prefab", boss.transform, 6.5f, ivory);
            visual.name = "Visual";
            var collider = boss.AddComponent<BoxCollider>();
            collider.center = Vector3.up * 3.25f; collider.size = new Vector3(3.2f, 6.5f, 3.2f);
            boss.AddComponent<BossTarget>();
            ConfigureBoss(boss, null);
            PrefabUtility.SaveAsPrefabAsset(boss, Root + "/PhaseOneBoss.prefab");
            Object.DestroyImmediate(boss);
        }

        public static void ConfigureBoss(GameObject boss, Transform target)
        {
            Add<BossTarget>(boss);
            Add<BossMovementStateController>(boss);
            var agent = boss.GetComponent<NavMeshAgent>();
            agent.radius = 1.6f; agent.height = 6.5f; agent.speed = 7f; agent.acceleration = 35f; agent.stoppingDistance = 7f;
            SetObject(boss.GetComponent<BossTarget>(), "_target", target);
            var health = Add<BossHealth>(boss);
            var runner = Add<BossPatternRunner>(boss);
            SetFloat(runner, "_patternInterval", 2f);
            Add<BossGroggyController>(boss);
            Add<BossPhaseController>(boss);
            Add<BossSummonRoster>(boss);
            var presentation = Add<BossCombatPresentation>(boss);
            var swords = Add<BossSwordController>(boss);
            SetObject(swords, "_swordPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/ThrownSword.prefab").GetComponent<ThrownBossSword>());

            Transform visual = boss.transform.Find("Visual");
            if (visual != null)
            {
                GameObject white = AssetDatabase.LoadAssetAtPath<GameObject>(Chess + "White King.prefab");
                MeshFilter current = visual.GetComponentInChildren<MeshFilter>();
                MeshFilter source = white.GetComponentInChildren<MeshFilter>();
                if (current != null && source != null) current.sharedMesh = source.sharedMesh;
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/WhiteChess.mat");
                SetObject(presentation, "_bodyVisual", visual);
            }
            Transform oldOrbit = boss.transform.Find("SwordOrbitCenter");
            if (oldOrbit != null) oldOrbit.gameObject.SetActive(false);
            Transform weapon = boss.transform.Find("PhaseOneWeapon");
            if (weapon == null)
            {
                weapon = new GameObject("PhaseOneWeapon").transform;
                weapon.SetParent(boss.transform, false); weapon.localPosition = Vector3.up * 3f;
                GameObject sword = CreateVisual("Assets/Members/CWH/PREFABS/Sword.prefab", weapon, 5f, null);
                sword.transform.localPosition = Vector3.forward;
                sword.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            }
            SetObject(presentation, "_weaponPivot", weapon);
            Material circle = AssetDatabase.LoadAssetAtPath<Material>(CircleMaterial);
            var melee = Add<MeleeComboPattern>(boss);
            SetObject(melee, "_telegraph", Area(boss, "Melee warning", circle));
            BaseTiming(melee, 0.9f, 1f, 3f);
            var summon = Add<SummonPattern>(boss);
            SetObject(summon, "_pawnPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PhaseOnePawn.prefab").GetComponent<ChessMinion>());
            SetObject(summon, "_knightPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PhaseOneKnight.prefab").GetComponent<ChessMinion>());
            SetObject(summon, "_firstWarning", Area(boss, "Summon warning A", circle));
            SetObject(summon, "_secondWarning", Area(boss, "Summon warning B", circle));
            BaseTiming(summon, 1.5f, 1.2f, 22f);
            var dash = Add<DashPattern>(boss);
            Transform dashPlane = boss.transform.Find("DashTelegraphEffect");
            if (dashPlane == null)
            {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "DashTelegraphEffect"; plane.transform.SetParent(boss.transform, false);
                Object.DestroyImmediate(plane.GetComponent<Collider>());
                dashPlane = plane.transform;
            }
            dashPlane.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DashMaterial);
            var dashBase = Add<BossTelegraphPresenter>(dashPlane.gameObject);
            SetObject(dashBase, "_targetRenderer", dashPlane.GetComponent<Renderer>());
            SetObject(dash, "_telegraph", Add<DashTelegraphPresenter>(dashPlane.gameObject));
            BaseTiming(dash, 1.2f, 1f, 10f);
            var throwing = Add<SwordThrowPattern>(boss);
            SetObject(throwing, "_telegraph", Area(boss, "Sword landing warning", circle));
            BaseTiming(throwing, 1.3f, 1f, 12f);
            bool newRoar = boss.GetComponent<RoarPattern>() == null;
            var roar = Add<RoarPattern>(boss);
            SetObject(roar, "_areaTelegraph", Area(boss, "Roar area warning", circle));
            BaseTiming(roar, 1.5f, 0.8f, 18f);
            if (newRoar) { SetFloat(roar, "_roarRadius", 10f); SetFloat(roar, "_knockbackSpeed", 35f); SetFloat(roar, "_knockbackDuration", 0.5f); }
            var patterns = new BossPattern[] { melee, summon, dash, throwing, roar };
            SetObjects(Add<PhaseOneBrain>(boss), "_patterns", patterns);
            SetObjects(Add<PhaseOneEncounter>(boss), "_phaseOnePatterns", patterns);
            if (target != null && target.GetComponent<TestCameraController>() != null) Add<TestDamageReceiver>(target.gameObject);
            EditorUtility.SetDirty(health);
        }

        private static void CreateMinion(string name, string visualName, ChessMinionKind kind, float height, Material material)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + name + ".prefab") != null) return;
            var root = new GameObject(name);
            CreateVisual(Chess + visualName + ".prefab", root.transform, height, material);
            var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * height * 0.5f; collider.size = new Vector3(1.6f, height, 1.6f);
            var agent = root.AddComponent<NavMeshAgent>(); agent.radius = 0.8f; agent.height = height; agent.speed = 3.5f; agent.acceleration = 15f; agent.stoppingDistance = 1.7f;
            SetFloat(root.AddComponent<BreakableHealth>(), "_maxHealth", kind == ChessMinionKind.Pawn ? 80f : 120f);
            var minion = root.AddComponent<ChessMinion>();
            SetInt(minion, "_kind", (int)kind);
            SetObject(minion, "_warningMaterial", AssetDatabase.LoadAssetAtPath<Material>(CircleMaterial));
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }
        private static void CreateSword()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/ThrownSword.prefab") != null) return;
            var root = new GameObject("ThrownSword");
            var pivot = new GameObject("Sword visual pivot"); pivot.transform.SetParent(root.transform, false);
            GameObject model = CreateVisual("Assets/Members/CWH/PREFABS/Sword.prefab", pivot.transform, 4f, null);
            model.transform.localPosition += Vector3.down * 4f;
            var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * 2f; collider.size = new Vector3(1.6f, 4f, 1.6f); collider.isTrigger = true;
            SetFloat(root.AddComponent<BreakableHealth>(), "_maxHealth", 60f);
            SetObject(root.AddComponent<ThrownBossSword>(), "_visual", pivot.transform);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/ThrownSword.prefab"); Object.DestroyImmediate(root);
        }
        private static GameObject CreateVisual(string path, Transform parent, float height, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new System.InvalidOperationException("필수 시각 자산 없음: " + path);
            GameObject visual = Object.Instantiate(source, parent);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) { bounds.Encapsulate(renderer.bounds); if (material != null) renderer.sharedMaterial = material; }
            visual.transform.position -= new Vector3(bounds.center.x - parent.position.x, bounds.min.y - parent.position.y, bounds.center.z - parent.position.z);
            return visual;
        }
        private static AreaTelegraphPresenter Area(GameObject boss, string name, Material material)
        {
            Transform old = boss.transform.Find(name);
            AreaTelegraphPresenter area = old != null ? Add<AreaTelegraphPresenter>(old.gameObject) : AreaTelegraphPresenter.Create(boss.transform, material, name);
            area.GetComponent<Renderer>().sharedMaterial = material;
            SetObject(area.GetComponent<BossTelegraphPresenter>(), "_targetRenderer", area.GetComponent<Renderer>());
            return area;
        }
        private static Material EnsureMaterial(string name, Color color)
        {
            string path = Root + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void MakeCube(string name, Vector3 position, Vector3 scale, Material material)
        { GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.position = position; cube.transform.localScale = scale; cube.GetComponent<Renderer>().sharedMaterial = material; }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/')); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
        private static T Add<T>(GameObject root) where T : Component => root.TryGetComponent(out T component) ? component : root.AddComponent<T>();
        private static void BaseTiming(BossPattern pattern, float warning, float recovery, float cooldown)
        { SetFloat(pattern, "_warningDuration", warning); SetFloat(pattern, "_recoveryDuration", recovery); SetFloat(pattern, "_cooldown", cooldown); }
        private static void SetFloat(Object owner, string field, float value) { var data = new SerializedObject(owner); data.FindProperty(field).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetInt(Object owner, string field, int value) { var data = new SerializedObject(owner); data.FindProperty(field).intValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetObject(Object owner, string field, Object value) { var data = new SerializedObject(owner); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetObjects(Object owner, string field, Object[] values)
        { var data = new SerializedObject(owner); var array = data.FindProperty(field); array.arraySize = values.Length; for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
