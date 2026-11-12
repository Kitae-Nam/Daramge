using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NKT.Enemy.EditorTools
{
    //제자리 달리기 클립에 앞으로 가는 루트 모션을 넣은 복사본을 만든다.
    //메뉴: Tools/NKT/Bake Mimic Run Root Motion
    //
    //EnemyNavRenderer는 Animator.rootPosition으로 적을 움직이는데, 미믹 Run은 제자리라서 안 움직인다.
    //Generic 리그에 Root Node가 없으면 루트 오브젝트(경로 "")의 위치 커브가 루트 모션이 되므로,
    //원본 커브는 그대로 복사하고 루트에 일정 속도로 앞으로(+Z) 가는 커브만 추가한다.
    //
    //속도는 발이 땅에 닿아 있는 동안 뒤로 미끄러지는 속도로 자동 계산한다(발이 안 미끄러지는 속도).
    //어색하면 OverrideSpeed에 값을 넣으면 그 속도를 쓴다.
    public static class InPlaceRootMotionBaker
    {
        private const string AssetRoot = "Assets/Members/NKT/04_Graphics/00_Assets/RPGMonsterPartnersPBRPolyart";
        private const string ModelPrefabPath = AssetRoot + "/Prefabs/Character/ChestMonsterPolyartDefault.prefab";
        private const string SourceClipPath = AssetRoot + "/Animations/Chest Monster/Run.fbx";
        private const string OutputPath = "Assets/Members/NKT/03_GameModules/Enemy/Enemies/Mimic/Mimic Run RootMotion.anim";

        private const float OverrideSpeed = 0f;   //0이면 자동 계산 (m/s)
        private const float FallbackSpeed = 2.5f; //자동 계산에 실패했을 때
        private const int SampleCount = 120;
        private const float ContactHeightRatio = 0.2f; //발 높이 범위 중 아래 20% 안이면 땅에 닿은 것으로 본다.

        [MenuItem("Tools/NKT/Bake Mimic Run Root Motion")]
        private static void Bake()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPrefabPath);
            AnimationClip source = AssetDatabase.LoadAllAssetsAtPath(SourceClipPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (prefab == null || source == null)
            {
                Debug.LogError($"모델이나 클립을 찾지 못했습니다.\n{ModelPrefabPath}\n{SourceClipPath}");
                return;
            }

            float speed = OverrideSpeed > 0f ? OverrideSpeed : EstimateSpeed(prefab, source);
            AnimationClip clip = CreateClip(source, speed);
            Save(clip);

            Debug.Log($"루트 모션 클립을 만들었습니다: {OutputPath}\n" +
                      $"전진 속도 {speed:0.00} m/s (한 루프 {source.length:0.00}초에 {speed * source.length:0.00}m). " +
                      "NavMeshAgent 속도(이동 속도)를 이 값 근처로 맞추면 발이 덜 미끄러집니다.", clip);
        }

        private static AnimationClip CreateClip(AnimationClip source, float speed)
        {
            AnimationClip clip = new AnimationClip { frameRate = source.frameRate };

            //원본 커브 그대로 복사 (루트 오브젝트 위치 커브는 새로 넣을 거라 제외)
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.path == "" && binding.type == typeof(Transform)
                                       && binding.propertyName.StartsWith("m_LocalPosition"))
                    continue;
                AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(source, binding));
            }

            //루트를 일정 속도로 앞으로. 접선을 속도와 같게 해서 직선(등속)으로 만든다.
            float length = source.length;
            SetRootCurve(clip, "m_LocalPosition.x", AnimationCurve.Constant(0f, length, 0f));
            SetRootCurve(clip, "m_LocalPosition.y", AnimationCurve.Constant(0f, length, 0f));
            SetRootCurve(clip, "m_LocalPosition.z", new AnimationCurve(
                new Keyframe(0f, 0f, speed, speed),
                new Keyframe(length, speed * length, speed, speed)));

            clip.EnsureQuaternionContinuity();
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));

            //루프 유지, 회전과 높이는 포즈에 굽고(Bake Into Pose) XZ만 루트 모션으로 쓴다. 다른 적 RUN 클립과 같은 설정.
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void SetRootCurve(AnimationClip clip, string property, AnimationCurve curve)
            => AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("", typeof(Transform), property), curve);

        //발(말단 뼈)이 땅에 닿아 있는 구간에서 뒤로 움직이는 속도 = 몸이 앞으로 가야 하는 속도
        private static float EstimateSpeed(GameObject prefab, AnimationClip clip)
        {
            GameObject instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            try
            {
                List<Transform> tips = FindLegTips(instance, clip);
                float dt = clip.length / SampleCount;

                Dictionary<Transform, Vector3[]> tracks = tips.ToDictionary(t => t, _ => new Vector3[SampleCount + 1]);
                for (int i = 0; i <= SampleCount; i++)
                {
                    clip.SampleAnimation(instance, i * dt);
                    foreach (Transform tip in tips)
                        tracks[tip][i] = tip.position;
                }

                List<float> speeds = new List<float>();
                foreach (Vector3[] track in tracks.Values)
                {
                    float minY = track.Min(p => p.y);
                    float maxY = track.Max(p => p.y);
                    if (maxY - minY < 0.01f) continue; //위아래로 안 움직이면 발이 아님

                    float contactY = minY + (maxY - minY) * ContactHeightRatio;
                    for (int i = 0; i < SampleCount; i++)
                    {
                        if (track[i].y > contactY || track[i + 1].y > contactY) continue;
                        float backward = -(track[i + 1].z - track[i].z) / dt;
                        if (backward > 0f) speeds.Add(backward);
                    }
                }

                if (speeds.Count == 0)
                {
                    Debug.LogWarning($"발 움직임으로 속도를 계산하지 못해서 기본값 {FallbackSpeed} m/s를 씁니다.");
                    return FallbackSpeed;
                }

                speeds.Sort();
                return speeds[speeds.Count / 2]; //튀는 값 영향을 줄이려고 중앙값
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        //애니메이션되는 뼈 중 자식이 없는 끝 뼈들 (다리 끝 후보)
        private static List<Transform> FindLegTips(GameObject instance, AnimationClip clip)
        {
            List<string> paths = AnimationUtility.GetCurveBindings(clip)
                .Where(b => b.type == typeof(Transform) && !string.IsNullOrEmpty(b.path))
                .Select(b => b.path)
                .Distinct()
                .ToList();

            return paths
                .Where(p => !paths.Any(other => other != p && other.StartsWith(p + "/")))
                .Select(p => instance.transform.Find(p))
                .Where(t => t != null)
                .ToList();
        }

        private static void Save(AnimationClip clip)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputPath);
            if (existing != null)
            {
                //컨트롤러에 연결된 참조를 유지하려고 내용만 덮어쓴다.
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
        }
    }
}
