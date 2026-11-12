#if UNITY_EDITOR
using System;
using System.Linq;
using CHG._02_Scripts.Player;
using CWH.SCRIPTS.Cinematics;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Members.CWH.SCRIPTS.Editor
{
    /// <summary>씬 연결과 에셋 제작만 담당한다. 재생 로직을 에디터 코드에 넣지 않아 빌드에서도 같은 연출을 쓴다.</summary>
    public static class PhaseCutsceneBuilder
    {
        public const string ScenePath = "Assets/Members/CWH/SCENCES/DEV_SCNE_2.unity";
        public const string Folder = "Assets/Members/CWH/TIMELINE";
        public const string TimelinePath = Folder + "/King_PhaseTwo_Reveal.playable";
        public const string RootName = "King PhaseTwo Cutscene";
        private const double Duration = 5.5;

        [MenuItem("CWH/Boss/2페이즈 컷신 생성 및 연결")]
        public static void Build()
        {
            // 열려 있는 대상 씬의 미저장 편집도 그대로 포함한다. 다른 씬이 열려 있으면 먼저 사용자가 대상을 열게 한다.
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("DEV_SCNE_2 씬을 먼저 열어 주세요.");
                scene = EditorSceneManager.OpenScene(ScenePath);
            }
            if (scene.GetRootGameObjects().Any(x => x.name == RootName))
                throw new InvalidOperationException("이미 연결된 컷신이 있습니다. 기존 연출을 편집하세요: " + TimelinePath);
            if (AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath) != null)
                throw new InvalidOperationException("기존 에셋을 덮어쓰지 않습니다: " + TimelinePath);
            var boss = Object.FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include)
                .Single(x => x.name == "The_King(Boss)Real");
            var appearance = boss.GetComponent<ChessPhaseAppearance>();
            var body = (Renderer)new SerializedObject(appearance).FindProperty("_bodyRenderer").objectReferenceValue;
            var brain = Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Exclude).Single();
            var intro = Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include)
                .Single(x => x.name == "Appearance TimeLine");
            var fallback = FindGameplayCamera();
            float height = Mathf.Max(0.5f, body.bounds.size.y);
            var root = new GameObject(RootName);
            // 등장 애니메이션의 대기 위치(Visual이 공중에 있음) 대신 전투 루트를 기준으로 미리보기 배치한다.
            root.transform.SetPositionAndRotation(boss.transform.position + Vector3.up * height * 0.5f,
                Quaternion.Euler(0, boss.transform.eulerAngles.y, 0));
            root.transform.localScale = Vector3.one * height;
            var director = root.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "King PhaseTwo Reveal";
            timeline.editorSettings.frameRate = 60;
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = Duration;
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            director.playableAsset = timeline;

            var rig = Child(root.transform, "CameraRig");
            rig.transform.rotation = fallback.transform.rotation;
            var animator = rig.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var cameraObject = Child(rig.transform, "PhaseTwoCamera");
            var camera = cameraObject.AddComponent<CinemachineCamera>();
            camera.Priority = -100;
            camera.Lens.FieldOfView = 42;
            camera.Lens.NearClipPlane = 0.05f;
            camera.Lens.FarClipPlane = 1000;
            // 카메라에는 Follow/Aim/Input/Noise를 붙이지 않는다. 부모 안의 로컬 애니메이션만 구도를 결정한다.
            var animation = CreateCameraAnimation(height);
            AssetDatabase.CreateAsset(animation, Folder + "/King_PhaseTwo_Camera.anim");
            var motionTrack = timeline.CreateTrack<AnimationTrack>(null, "02 | Camera - approach, charge, recoil");
            motionTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
            var motionClip = motionTrack.CreateClip<AnimationPlayableAsset>();
            ((AnimationPlayableAsset)motionClip.asset).clip = animation;
            ((AnimationPlayableAsset)motionClip.asset).removeStartOffset = false;
            motionClip.duration = Duration;
            motionClip.displayName = "Local camera choreography (boss height = 1)";
            director.SetGenericBinding(motionTrack, animator);
            animation.SampleAnimation(rig, 0f);

            var shots = timeline.CreateTrack<CinemachineTrack>(null, "01 | Camera - gameplay > reveal > gameplay");
            shots.TrackPriority = 20;
            director.SetGenericBinding(shots, brain);
            AddShot(shots, director, "Gameplay / entry", 0, 1.0, fallback, BossPhaseCutscene.GameplayCameraReference);
            AddShot(shots, director, "King / phase reveal", 0.1, 4.8, camera, "PhaseTwo.RevealCamera");
            AddShot(shots, director, "Gameplay / return", 4.3, 1.2, fallback, BossPhaseCutscene.GameplayCameraReference);

            AddEffect(timeline, director, root.transform, "03 | Gold seal", "Assets/Epic Toon FX/Prefabs/Combat/Magic/Circle Simple/MagicCircleSimpleYellow.prefab",
                new Vector3(0, -0.47f, 0), 0.7f, 0.35, 2.6, null);
            AddEffect(timeline, director, root.transform, "04 | Violet charge", "Assets/Epic Toon FX/Prefabs/Combat/Magic/Charge/MagicChargeBlue.prefab",
                new Vector3(0, 0.04f, 0), 0.65f, 0.6, 2.05, new Color(0.75f, 0.25f, 1f));
            AddEffect(timeline, director, root.transform, "05 | Dark fire impact", "Assets/Members/CWH/EFFECT ASSETS/EffectCore/packs/StylizedExplosionPack1/prefabs/ShaderGraphVersion/purple_darkFire_ShaderGraph/explosion_stylized_medium_darkFire_ShaderGraph.prefab",
                new Vector3(0, -0.35f, 0), 0.55f, 2.65, 2.1, null);
            AddEffect(timeline, director, root.transform, "06 | Awakened aura", "Assets/Epic Toon FX/Prefabs/Combat/Magic/Aura/MagicAuraBlue.prefab",
                new Vector3(0, -0.42f, 0), 0.65f, 2.65, 2.3, new Color(0.65f, 0.2f, 1f));

            var signal = ScriptableObject.CreateInstance<SignalAsset>();
            AssetDatabase.CreateAsset(signal, Folder + "/King_PhaseTwo_Reveal.signal");
            var signals = timeline.CreateTrack<SignalTrack>(null, "07 | Reveal black king at impact");
            var marker = signals.CreateMarker<SignalEmitter>(2.65);
            marker.asset = signal;
            marker.retroactive = true;
            marker.emitOnce = true;
            var receiver = root.AddComponent<SignalReceiver>();
            var reveal = new UnityEngine.Events.UnityEvent();
            UnityEventTools.AddPersistentListener(reveal, appearance.RevealPhaseTwo);
            receiver.AddReaction(signal, reveal);
            director.SetGenericBinding(signals, receiver);
            Set(appearance, "_deferToCutscene", true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(appearance);

            var controller = root.AddComponent<BossPhaseCutscene>();
            Set(controller, "_phase", boss);
            Set(controller, "_combatGate", boss.GetComponent<BossCombatGate>());
            Set(controller, "_health", boss.GetComponent<BossHealth>());
            Set(controller, "_body", body);
            Set(controller, "_appearance", appearance);
            Set(controller, "_intro", intro);
            Set(controller, "_brain", brain);
            Set(controller, "_fallbackGameplayCamera", fallback);
            Set(controller, "_cutsceneCamera", camera);
            var characterChange = Object.FindAnyObjectByType<CharacterChange>();
            if (characterChange != null)
            {
                // 플레이어 구현에 재생 컴포넌트를 결합하지 않고 이벤트로 기존 입력 API를 연결한다.
                var started = (UnityEngine.Events.UnityEvent)typeof(BossPhaseCutscene)
                    .GetField("_started", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
                var finished = (UnityEngine.Events.UnityEvent)typeof(BossPhaseCutscene)
                    .GetField("_finished", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
                UnityEventTools.AddPersistentListener(started, characterChange.LockInput);
                UnityEventTools.AddPersistentListener(finished, characterChange.UnlockInput);
                EditorUtility.SetDirty(controller);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("KING_PHASE_CUTSCENE_BUILT " + TimelinePath);
        }

        private static CinemachineVirtualCameraBase FindGameplayCamera()
        {
            var change = Object.FindAnyObjectByType<CharacterChange>();
            if (change != null)
            {
                var data = new SerializedObject(change);
                var entry = data.FindProperty("settings").GetArrayElementAtIndex(data.FindProperty("startIndex").intValue);
                return (CinemachineVirtualCameraBase)entry.FindPropertyRelative("camera").objectReferenceValue;
            }
            return Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude)
                .Where(x => x.Follow != null).OrderByDescending(x => x.Priority.Value).First();
        }

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void AddShot(CinemachineTrack track, PlayableDirector director, string name,
            double start, double duration, CinemachineVirtualCameraBase camera, string reference)
        {
            var clip = track.CreateClip<CinemachineShot>();
            clip.start = start;
            clip.duration = duration;
            clip.displayName = name;
            var shot = (CinemachineShot)clip.asset;
            shot.VirtualCamera.exposedName = reference;
            director.SetReferenceValue(reference, camera);
        }

        private static void AddEffect(TimelineAsset timeline, PlayableDirector director, Transform parent,
            string name, string path, Vector3 position, float scale, double start, double duration, Color? tint)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("VFX prefab missing: " + path);
            var effect = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            effect.name = name.Substring(5);
            effect.transform.localPosition = position;
            effect.transform.localScale = Vector3.one * scale;
            // 프리팹의 -90도 보정 등 제작 당시 회전은 보존한다. Timeline이 수명/활성을 소유하므로 자동 파괴는 끈다.
            foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (tint.HasValue) main.startColor = tint.Value;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                PrefabUtility.RecordPrefabInstancePropertyModifications(particle);
            }
            effect.SetActive(false);
            var track = timeline.CreateTrack<ControlTrack>(null, name);
            var clip = track.CreateClip<ControlPlayableAsset>();
            clip.start = start;
            clip.duration = duration;
            clip.displayName = effect.name;
            var control = (ControlPlayableAsset)clip.asset;
            control.sourceGameObject.exposedName = "PhaseTwo." + name;
            control.updateDirector = false;
            control.updateITimeControl = false;
            control.searchHierarchy = true;
            control.active = true;
            control.postPlayback = ActivationControlPlayable.PostPlaybackState.Inactive;
            control.particleRandomSeed = 7611;
            director.SetReferenceValue(control.sourceGameObject.exposedName, effect);
        }

        [MenuItem("CWH/Boss/2페이즈 컷신 렌즈 줌 보정")]
        public static void CalibrateLensZoom()
        {
            var controller = Object.FindAnyObjectByType<BossPhaseCutscene>();
            if (controller == null) throw new InvalidOperationException("연결된 컷신이 없습니다.");
            var body = (Renderer)new SerializedObject(controller).FindProperty("_body").objectReferenceValue;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/King_PhaseTwo_Camera.anim");
            AddOrthographicZoom(clip, Mathf.Max(0.5f, body.bounds.size.y));
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip CreateCameraAnimation(float height)
        {
            var clip = new AnimationClip { name = "King PhaseTwo Camera", frameRate = 60 };
            float[] times = { 0f, 1f, 2.45f, 2.65f, 2.76f, 3.25f, 4.3f, 5.5f };
            // 리그가 플레이어 캠의 월드 각도를 소유한다. 자식은 시선의 뒤쪽(-Z)에서 직선으로 접근한다.
            // 측면 오프셋과 별도 LookRotation을 제거해 줌 중에도 플레이어 캠의 내려다보는 각도를 유지한다.
            float[] distances = { 3.56f, 2.22f, 1.83f, 1.83f, 2.14f, 2.42f, 3.06f, 3.06f };
            var positions = distances.Select(x => Vector3.back * x).ToArray();
            float[] fovs = { 45, 40, 36, 36, 42, 40, 45, 45 };
            // 시점 회전은 리그에만 있으므로 자식의 회전 키는 항상 항등 회전으로 유지한다.
            var rotations = Enumerable.Repeat(Quaternion.identity, times.Length).ToArray();
            for (int axis = 0; axis < 3; axis++)
                Curve(clip, typeof(Transform), "m_LocalPosition." + "xyz"[axis], times, positions.Select(x => x[axis]).ToArray());
            for (int axis = 0; axis < 4; axis++)
                Curve(clip, typeof(Transform), "m_LocalRotation." + "xyzw"[axis], times, rotations.Select(x => x[axis]).ToArray());
            Curve(clip, typeof(CinemachineCamera), "Lens.FieldOfView", times, fovs);
            AddOrthographicZoom(clip, height);
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        [MenuItem("CWH/Boss/2페이즈 컷신을 플레이어 시점으로 맞추기")]
        public static void MatchGameplayView()
        {
            var controller = Object.FindAnyObjectByType<BossPhaseCutscene>();
            if (controller == null) throw new InvalidOperationException("연결된 컷신이 없습니다.");
            var data = new SerializedObject(controller);
            var body = (Renderer)data.FindProperty("_body").objectReferenceValue;
            var gameplay = (CinemachineVirtualCameraBase)data.FindProperty("_fallbackGameplayCamera").objectReferenceValue;
            var camera = (CinemachineCamera)data.FindProperty("_cutsceneCamera").objectReferenceValue;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/King_PhaseTwo_Camera.anim");
            // 에셋 참조와 GUID를 유지한 채 곡선만 갱신해 기존 Timeline 바인딩을 보존한다.
            var replacement = CreateCameraAnimation(Mathf.Max(0.5f, body.bounds.size.y));
            EditorUtility.CopySerialized(replacement, clip);
            Object.DestroyImmediate(replacement);
            camera.transform.parent.rotation = gameplay.transform.rotation;
            clip.SampleAnimation(camera.transform.parent.gameObject, 0f);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }

        private static void AddOrthographicZoom(AnimationClip clip, float height)
        {
            // 기존 게임플레이가 직교 카메라이므로 이동/FOV만으로는 화면 크기가 변하지 않는다.
            // 투영 방식을 바꾸면 블렌드 중 구도가 튈 수 있어 직교 크기도 보스 높이에 맞춰 직접 애니메이션한다.
            float[] times = { 0f, 1f, 2.45f, 2.65f, 2.76f, 3.25f, 4.3f, 5.5f };
            // 시작/복귀 크기를 플레이어 캠의 약 5에 맞춰 진입 직후 화면이 먼저 멀어지는 현상을 피한다.
            float[] sizes = { 1.45f, 1.05f, 0.7f, 0.7f, 0.85f, 1f, 1.45f, 1.45f };
            Curve(clip, typeof(CinemachineCamera), "Lens.OrthographicSize", times, sizes.Select(x => x * height).ToArray());
        }

        private static void Curve(AnimationClip clip, Type type, string property, float[] times, float[] values)
        {
            var curve = new AnimationCurve(times.Select((t, i) => new Keyframe(t, values[i])).ToArray());
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            clip.SetCurve("PhaseTwoCamera", type, property, curve);
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string property, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
