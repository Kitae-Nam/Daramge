using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NKT.Enemy.EditorTools
{
    //상자(IdleChest 첫 프레임) -> 괴물(IdleNormal 첫 프레임)로 변신하는 클립을 만든다.
    //메뉴: Tools/NKT/Build Mimic Transform Clip
    //
    //타임라인 (초)
    //  0.00 ~ RattleEnd   : 상자 모습으로 덜컹거림 (좌우로 흔들리고 살짝 뛴다)
    //  RattleEnd ~ BurstStart : 잠깐 멈춤 (긴장감)
    //  BurstStart ~ BurstEnd  : 괴물 모습으로 튀어나옴 (EaseOutBack이라 살짝 넘쳤다가 자리 잡음)
    //  BurstEnd ~ Duration    : 괴물 모습 유지 (IdleNormal 첫 프레임과 같아서 다음 상태로 끊김 없이 이어짐)
    //
    //두 클립에서 포즈를 직접 샘플링해서 보간하기 때문에 뼈 이름이나 회전 축을 몰라도 된다.
    public static class MimicTransformClipBuilder
    {
        private const string AssetRoot = "Assets/Members/NKT/04_Graphics/00_Assets/RPGMonsterPartnersPBRPolyart";
        private const string ModelPrefabPath = AssetRoot + "/Prefabs/Character/ChestMonsterPolyartDefault.prefab";
        private const string ChestClipPath = AssetRoot + "/Animations/Chest Monster/IdleChest.fbx";
        private const string MonsterClipPath = AssetRoot + "/Animations/Chest Monster/IdleNormal.fbx";
        private const string OutputPath = "Assets/Members/NKT/03_GameModules/Enemy/Enemies/Mimic/Mimic Transform.anim";

        private const float SampleRate = 60f;
        private const float RattleEnd = 0.45f;
        private const float BurstStart = 0.52f;
        private const float BurstEnd = 0.95f;
        private const float Duration = 1.05f;

        private const float RattleAngle = 7f;        //덜컹거릴 때 최대 기울기(도)
        private const float RattleFrequency = 13f;   //초당 흔들림 횟수
        private const float HopHeightRatio = 0.035f; //모델 높이 대비 뛰는 높이
        private const float Overshoot = 2.2f;        //EaseOutBack 넘침 정도 (0이면 넘치지 않음)

        private struct Pose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        [MenuItem("Tools/NKT/Build Mimic Transform Clip")]
        private static void Build()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPrefabPath);
            AnimationClip chestClip = LoadClip(ChestClipPath);
            AnimationClip monsterClip = LoadClip(MonsterClipPath);
            if (prefab == null || chestClip == null || monsterClip == null)
            {
                Debug.LogError($"모델이나 클립을 찾지 못했습니다.\n{ModelPrefabPath}\n{ChestClipPath}\n{MonsterClipPath}");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                AnimationClip clip = CreateClip(instance, chestClip, monsterClip);
                SaveClip(clip);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static AnimationClip CreateClip(GameObject instance, AnimationClip chestClip, AnimationClip monsterClip)
        {
            //두 클립 중 하나라도 움직이는 뼈만 키를 넣는다. 루트(경로 "")는 오브젝트 위치라서 제외.
            List<string> paths = AnimationUtility.GetCurveBindings(chestClip)
                .Concat(AnimationUtility.GetCurveBindings(monsterClip))
                .Where(b => b.type == typeof(Transform) && !string.IsNullOrEmpty(b.path))
                .Select(b => b.path)
                .Distinct()
                .Where(p => instance.transform.Find(p) != null)
                .ToList();

            Dictionary<string, Pose> chestPose = SamplePose(instance, chestClip, paths);
            Dictionary<string, Pose> monsterPose = SamplePose(instance, monsterClip, paths);

            //덜컹거림은 가장 위쪽 뼈 하나에만 준다. 축은 월드 기준으로 계산해서 뼈의 로컬 축과 상관없게 한다.
            string shakePath = paths.OrderBy(p => p.Count(c => c == '/')).First();
            chestClip.SampleAnimation(instance, 0f);
            Transform shakeBone = instance.transform.Find(shakePath);
            Quaternion parentRotation = shakeBone.parent.rotation;
            Vector3 rollAxisLocal = Quaternion.Inverse(parentRotation) * Vector3.forward;
            Vector3 upLocal = shakeBone.parent.InverseTransformVector(Vector3.up);
            float hopHeight = GetModelHeight(instance) * HopHeightRatio;

            AnimationClip clip = new AnimationClip { frameRate = SampleRate };
            int frameCount = Mathf.RoundToInt(Duration * SampleRate);

            foreach (string path in paths)
            {
                AnimationCurve[] curves = Enumerable.Range(0, 10).Select(_ => new AnimationCurve()).ToArray();
                Quaternion previous = chestPose[path].Rotation;

                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = frame / SampleRate;
                    Pose pose = Evaluate(chestPose[path], monsterPose[path], time);

                    if (path == shakePath && time < RattleEnd)
                    {
                        (float angle, float hop) = Rattle(time);
                        pose.Rotation = Quaternion.AngleAxis(angle, rollAxisLocal) * pose.Rotation;
                        pose.Position += upLocal * (hop * hopHeight);
                    }

                    //쿼터니언은 q와 -q가 같은 회전이라, 이전 키와 반대 부호면 뒤집어서 보간이 튀지 않게 한다.
                    if (Quaternion.Dot(previous, pose.Rotation) < 0f)
                        pose.Rotation = new Quaternion(-pose.Rotation.x, -pose.Rotation.y, -pose.Rotation.z, -pose.Rotation.w);
                    previous = pose.Rotation;

                    AddKeys(curves, time, pose);
                }

                SetCurves(clip, path, curves);
            }

            clip.EnsureQuaternionContinuity();
            return clip;
        }

        private static Pose Evaluate(Pose chest, Pose monster, float time)
        {
            if (time <= BurstStart) return chest;
            if (time >= BurstEnd) return monster;

            float t = EaseOutBack((time - BurstStart) / (BurstEnd - BurstStart));
            return new Pose
            {
                Position = Vector3.LerpUnclamped(chest.Position, monster.Position, t),
                Rotation = Quaternion.SlerpUnclamped(chest.Rotation, monster.Rotation, t),
                //스케일이 0에서 커지는 부위(다리 등)가 음수로 뒤집히지 않게 0 아래로는 안 내려간다.
                Scale = Vector3.Max(Vector3.zero, Vector3.LerpUnclamped(chest.Scale, monster.Scale, t))
            };
        }

        //점점 세지는 좌우 흔들림 + 흔들림 박자에 맞춘 작은 점프
        private static (float angle, float hop) Rattle(float time)
        {
            float progress = time / RattleEnd;
            float envelope = Mathf.Sin(progress * Mathf.PI) * Mathf.Lerp(0.5f, 1f, progress);
            float wave = Mathf.Sin(time * RattleFrequency * Mathf.PI * 2f);
            float angle = wave * RattleAngle * envelope;
            float hop = Mathf.Abs(wave) * envelope;
            return (angle, hop);
        }

        private static float EaseOutBack(float t)
        {
            float c3 = Overshoot + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + Overshoot * x * x;
        }

        private static Dictionary<string, Pose> SamplePose(GameObject instance, AnimationClip clip, List<string> paths)
        {
            clip.SampleAnimation(instance, 0f);
            Dictionary<string, Pose> pose = new Dictionary<string, Pose>();
            foreach (string path in paths)
            {
                Transform bone = instance.transform.Find(path);
                pose[path] = new Pose
                {
                    Position = bone.localPosition,
                    Rotation = bone.localRotation,
                    Scale = bone.localScale
                };
            }
            return pose;
        }

        private static void AddKeys(AnimationCurve[] curves, float time, Pose pose)
        {
            float[] values =
            {
                pose.Position.x, pose.Position.y, pose.Position.z,
                pose.Rotation.x, pose.Rotation.y, pose.Rotation.z, pose.Rotation.w,
                pose.Scale.x, pose.Scale.y, pose.Scale.z
            };
            for (int i = 0; i < values.Length; i++)
                curves[i].AddKey(new Keyframe(time, values[i]));
        }

        private static readonly string[] PropertyNames =
        {
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z"
        };

        private static void SetCurves(AnimationClip clip, string path, AnimationCurve[] curves)
        {
            for (int i = 0; i < curves.Length; i++)
            {
                for (int k = 0; k < curves[i].length; k++)
                    curves[i].SmoothTangents(k, 0f);

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), PropertyNames[i]);
                AnimationUtility.SetEditorCurve(clip, binding, curves[i]);
            }
        }

        private static float GetModelHeight(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return Mathf.Max(0.1f, bounds.size.y);
        }

        private static AnimationClip LoadClip(string fbxPath)
            => AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        private static void SaveClip(AnimationClip clip)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; //한 번만 재생하고 다음 상태로 넘어간다.
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            //다른 적 클립처럼 끝에 AnimationEndTrigger를 넣어서 BT의 WaitForAnimationEnd가 받을 수 있게 한다.
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent { time = Duration - 0.01f, functionName = "AnimationEndTrigger" }
            });

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputPath);
            if (existing != null)
            {
                //이미 있으면 내용만 덮어써서 컨트롤러에 연결된 참조(GUID)를 유지한다.
                EditorUtility.CopySerialized(clip, existing);
                existing.name = System.IO.Path.GetFileNameWithoutExtension(OutputPath);
                EditorUtility.SetDirty(existing);
                clip = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(clip, OutputPath);
            }

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(clip);
            Debug.Log($"변신 클립을 만들었습니다: {OutputPath} ({Duration:0.00}초)", clip);
        }
    }
}
