using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NKT.Enemy.EditorTools
{
    //뼈 없는 오리가미 동물(Runemark Origami Pack)용 애니메이션 클립을 만들고 Rush 컨트롤러에 넣는다.
    //에디터가 스크립트를 처음 컴파일할 때 클립이 없으면 자동으로 한 번 만든다.
    //다시 만들고 싶으면 메뉴: Tools/NKT/Build Origami Enemy Clips (기존 클립은 내용만 덮어써서 연결이 유지된다)
    //
    //필요한 계층 (클립은 "Model" 경로만 움직인다. 13마리 전부 같은 클립을 쓸 수 있다)
    //  적 루트 (AbstractEnemy, NavMeshAgent, BT)
    //  └ Visual (Animator, EnemyNavRenderer, AgentTrigger)   ← RUN 클립의 루트 모션(앞으로 가는 커브)만 여기
    //     └ Model                                          ← 통통 튀고 기울고 찌그러지는 건 전부 여기
    //        └ 오리가미 프리팹 (Y 90° 돌려서 머리가 +Z를 보게)
    //
    //Model의 기본값은 위치 0, 회전 0, 스케일 1이어야 한다. 방향 보정은 그 아래 프리팹에서 한다.
    [InitializeOnLoad]
    public static class OrigamiClipBuilder
    {
        private const string RushFolder = "Assets/Members/NKT/03_GameModules/00_Enemy/Enemies/Rush";
        private const string ClipFolder = RushFolder + "/Animations";
        private const string ControllerPath = RushFolder + "/Rush Enemy animation controller.controller";
        private const string DashParamPath = RushFolder + "/Dash Param.asset";
        private const string WindupParamPath = RushFolder + "/Dash Windup Param.asset";

        private const string ModelPath = "Model";
        private const float RunSpeed = 3.5f; //RUN 루트 모션 전진 속도(m/s). 바꾸면 메뉴로 다시 만들기
        private const string EndEventName = "AnimationEndTrigger";

        static OrigamiClipBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        private static void BuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!AssetDatabase.IsValidFolder(RushFolder)) return;
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath("IDLE")) != null) return;
            Build();
        }

        #region 클립 정의

        //위치는 Model 기준(m), 회전은 오일러(도): pitch +면 앞으로 숙임, roll +면 왼쪽으로 기울어짐.
        private struct Pose
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 Scale;
        }

        private static Pose P(float y = 0f, float z = 0f, float pitch = 0f, float roll = 0f,
            float sx = 1f, float sy = 1f, float sz = 1f)
            => new Pose
            {
                Position = new Vector3(0f, y, z),
                Rotation = new Vector3(pitch, 0f, roll),
                Scale = new Vector3(sx, sy, sz)
            };

        private class ClipDef
        {
            public string State;
            public float Length;
            public bool Loop;
            public bool EndEvent;
            public bool RootMotionForward;
            public (float time, Pose pose)[] Keys;
        }

        private static IEnumerable<ClipDef> Definitions()
        {
            //숨 쉬듯 살짝 눌렸다 펴짐
            yield return new ClipDef
            {
                State = "IDLE", Length = 1.6f, Loop = true,
                Keys = new[]
                {
                    (0f, P()),
                    (0.8f, P(pitch: -2f, sx: 1.03f, sy: 0.96f, sz: 1.03f)),
                    (1.6f, P()),
                }
            };

            //발견: 깜짝 놀라 펄쩍
            yield return new ClipDef
            {
                State = "NOTICE", Length = 0.6f, EndEvent = true,
                Keys = new[]
                {
                    (0f, P()),
                    (0.1f, P(sx: 1.12f, sy: 0.82f, sz: 1.12f)),
                    (0.25f, P(y: 0.45f, pitch: -12f, sx: 0.92f, sy: 1.12f, sz: 0.92f)),
                    (0.4f, P(sx: 1.1f, sy: 0.88f, sz: 1.1f)),
                    (0.6f, P()),
                }
            };

            //콩콩 두 번 뛰는 한 사이클. 착지 때 찌그러지고, 뛸 때 늘어나고, 좌우로 번갈아 기울어진다.
            yield return new ClipDef
            {
                State = "RUN", Length = 0.7f, Loop = true, RootMotionForward = true,
                Keys = new[]
                {
                    (0f, P(pitch: 6f, sx: 1.08f, sy: 0.88f, sz: 1.08f)),
                    (0.07f, P(y: 0.05f, pitch: -6f, roll: 3f, sx: 0.95f, sy: 1.1f, sz: 0.95f)),
                    (0.17f, P(y: 0.28f, pitch: -4f, roll: 5f)),
                    (0.29f, P(y: 0.06f, pitch: 8f, roll: 2f, sx: 0.97f, sy: 1.05f, sz: 0.97f)),
                    (0.35f, P(pitch: 6f, sx: 1.08f, sy: 0.88f, sz: 1.08f)),
                    (0.42f, P(y: 0.05f, pitch: -6f, roll: -3f, sx: 0.95f, sy: 1.1f, sz: 0.95f)),
                    (0.52f, P(y: 0.28f, pitch: -4f, roll: -5f)),
                    (0.64f, P(y: 0.06f, pitch: 8f, roll: -2f, sx: 0.97f, sy: 1.05f, sz: 0.97f)),
                    (0.7f, P(pitch: 6f, sx: 1.08f, sy: 0.88f, sz: 1.08f)),
                }
            };

            //뒤로 휙 젖혔다가 돌아옴
            yield return new ClipDef
            {
                State = "HIT", Length = 0.35f, EndEvent = true,
                Keys = new[]
                {
                    (0f, P()),
                    (0.06f, P(z: -0.15f, pitch: -18f, sx: 1.1f, sy: 0.9f, sz: 1.1f)),
                    (0.18f, P(pitch: 6f, sx: 0.97f, sy: 1.04f, sz: 0.97f)),
                    (0.35f, P()),
                }
            };

            //펄쩍 → 옆으로 쓰러짐 → 종이처럼 납작
            //옆으로 90° 누우면 Model의 X축이 위를 향하므로 X 스케일을 줄여야 위아래로 납작해진다.
            yield return new ClipDef
            {
                State = "DEAD", Length = 1.0f, EndEvent = true,
                Keys = new[]
                {
                    (0f, P()),
                    (0.12f, P(y: 0.2f, pitch: -15f, sx: 1.05f, sy: 0.95f, sz: 1.05f)),
                    (0.45f, P(y: 0.25f, roll: 88f)),
                    (0.6f, P(y: 0.2f, roll: 80f, sx: 0.85f, sy: 1.08f, sz: 1.08f)),
                    (1.0f, P(y: 0.08f, roll: 90f, sx: 0.25f, sy: 1.1f, sz: 1.1f)),
                }
            };

            //돌진 예고: 뒤로 웅크린 채 부르르 (예고 시간이 몇 초든 이어지도록 루프)
            yield return new ClipDef
            {
                State = "DASH_WINDUP", Length = 0.3f, Loop = true,
                Keys = new[]
                {
                    (0f, P(z: -0.1f, pitch: -12f, sx: 1.08f, sy: 0.85f, sz: 1.08f)),
                    (0.075f, P(y: 0.02f, z: -0.1f, pitch: -12f, roll: 4f, sx: 1.08f, sy: 0.85f, sz: 1.08f)),
                    (0.15f, P(z: -0.1f, pitch: -12f, sx: 1.08f, sy: 0.85f, sz: 1.08f)),
                    (0.225f, P(y: 0.02f, z: -0.1f, pitch: -12f, roll: -4f, sx: 1.08f, sy: 0.85f, sz: 1.08f)),
                    (0.3f, P(z: -0.1f, pitch: -12f, sx: 1.08f, sy: 0.85f, sz: 1.08f)),
                }
            };

            //돌진: 앞으로 숙이고 앞뒤로 길게 늘어남. 이동은 DashSkill이 직접 하므로 루트 모션 없음.
            yield return new ClipDef
            {
                State = "DASH", Length = 0.2f, Loop = true,
                Keys = new[]
                {
                    (0f, P(y: 0.05f, pitch: 15f, sx: 0.9f, sy: 0.9f, sz: 1.25f)),
                    (0.1f, P(y: 0.09f, pitch: 13f, sx: 0.9f, sy: 0.92f, sz: 1.22f)),
                    (0.2f, P(y: 0.05f, pitch: 15f, sx: 0.9f, sy: 0.9f, sz: 1.25f)),
                }
            };
        }

        #endregion

        [MenuItem("Tools/NKT/Build Origami Enemy Clips")]
        private static void Build()
        {
            EnsureFolder();

            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
            foreach (ClipDef def in Definitions())
                clips[def.State] = Save(CreateClip(def), ClipPath(def.State));

            SetupController(clips);
            SetParamName(DashParamPath, "DASH");
            SetParamName(WindupParamPath, "DASH_WINDUP");

            AssetDatabase.SaveAssets();
            Debug.Log($"[Origami] 클립 {clips.Count}개를 만들고 Rush 컨트롤러에 연결했습니다: {ClipFolder}\n" +
                      $"계층: Visual(Animator) > {ModelPath} > 오리가미 프리팹. RUN 전진 속도 {RunSpeed} m/s");
        }

        private static AnimationClip CreateClip(ClipDef def)
        {
            AnimationClip clip = new AnimationClip { frameRate = 60f };

            Vector3[] positions = def.Keys.Select(k => k.pose.Position).ToArray();
            Vector3[] rotations = def.Keys.Select(k => k.pose.Rotation).ToArray();
            Vector3[] scales = def.Keys.Select(k => k.pose.Scale).ToArray();
            float[] times = def.Keys.Select(k => k.time).ToArray();

            SetVectorCurves(clip, "m_LocalPosition", times, positions);
            SetVectorCurves(clip, "localEulerAnglesRaw", times, rotations); //애니메이션 창에서 녹화한 것과 같은 오일러 커브
            SetVectorCurves(clip, "m_LocalScale", times, scales);

            if (def.RootMotionForward)
            {
                //루트(Visual)를 일정 속도로 앞으로. EnemyNavRenderer가 이걸 루트 모션으로 받아서 적을 움직인다.
                SetCurve(clip, "", "m_LocalPosition.x", AnimationCurve.Constant(0f, def.Length, 0f));
                SetCurve(clip, "", "m_LocalPosition.y", AnimationCurve.Constant(0f, def.Length, 0f));
                SetCurve(clip, "", "m_LocalPosition.z", new AnimationCurve(
                    new Keyframe(0f, 0f, RunSpeed, RunSpeed),
                    new Keyframe(def.Length, RunSpeed * def.Length, RunSpeed, RunSpeed)));
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = def.Loop;
            //회전과 높이는 포즈에 굽고 XZ만 루트 모션으로 쓴다 (미믹 RUN과 같은 설정)
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (def.EndEvent)
            {
                AnimationUtility.SetAnimationEvents(clip, new[]
                {
                    new AnimationEvent { time = def.Length - 0.01f, functionName = EndEventName }
                });
            }
            return clip;
        }

        private static readonly string[] Axes = { "x", "y", "z" };

        private static void SetVectorCurves(AnimationClip clip, string property, float[] times, Vector3[] values)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                AnimationCurve curve = new AnimationCurve();
                for (int i = 0; i < times.Length; i++)
                    curve.AddKey(new Keyframe(times[i], values[i][axis]));

                //부드럽게 이어지되 키 값을 넘어가지 않게 (애니메이션 창 기본값과 같은 Clamped Auto)
                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                }

                SetCurve(clip, ModelPath, $"{property}.{Axes[axis]}", curve);
            }
        }

        private static void SetCurve(AnimationClip clip, string path, string property, AnimationCurve curve)
            => AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);

        private static AnimationClip Save(AnimationClip clip, string path)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                return clip;
            }

            //이미 있으면 내용만 덮어써서 컨트롤러에 연결된 참조(GUID)를 유지한다.
            EditorUtility.CopySerialized(clip, existing);
            existing.name = System.IO.Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        //상태 이름 = AnimParamSO의 ParamName (PlayClip이 CrossFade(상태 이름 해시)로 재생하므로)
        private static void SetupController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"[Origami] 컨트롤러가 없어서 클립만 만들었습니다: {ControllerPath}");
                return;
            }

            if (controller.parameters.All(p => p.name != "Speed"))
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            int row = 0;
            foreach (KeyValuePair<string, AnimationClip> pair in clips)
            {
                AnimatorState state = stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == pair.Key)
                                      ?? stateMachine.AddState(pair.Key, new Vector3(300f, 60f * row, 0f));
                state.motion = pair.Value;
                state.writeDefaultValues = true;
                row++;
            }

            if (stateMachine.defaultState == null)
                stateMachine.defaultState = stateMachine.states.Select(s => s.state).First(s => s.name == "IDLE");

            EditorUtility.SetDirty(controller);
        }

        //비어 있는 파라미터 SO에만 이름과 해시를 넣는다. (ParamName은 private set이라 SerializedObject로)
        private static void SetParamName(string path, string paramName)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) return;

            SerializedObject so = new SerializedObject(asset);
            SerializedProperty nameProperty = so.FindProperty("<ParamName>k__BackingField");
            SerializedProperty hashProperty = so.FindProperty("<ParamHash>k__BackingField");
            if (nameProperty == null || hashProperty == null || !string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            nameProperty.stringValue = paramName;
            hashProperty.intValue = Animator.StringToHash(paramName);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string ClipPath(string state) => $"{ClipFolder}/Origami {state}.anim";

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(ClipFolder))
                AssetDatabase.CreateFolder(RushFolder, "Animations");
        }
    }
}
