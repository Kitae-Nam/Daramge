using System.Collections.Generic;
using System.Linq;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using NKT.Enemy.Module;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace NKT.Enemy.EditorTools
{
    //적 루트모션 일괄 설정. 메뉴: Tools/NKT/Enemy Root Motion Setup
    //열려있는 씬의 적을 기준으로 Animator 블렌드 트리, 이동 클립 임포트 설정, 렌더러 교체, 에이전트 값을 맞춘다.
    //여러 번 실행해도 같은 결과가 나온다.
    public static class EnemyRootMotionSetup
    {
        private const string SpeedParamName = "Speed";
        private const string SpeedParamAssetPath = "Assets/Members/NKT/03_GameModules/Enemy/Animation/Enemy Speed Param.asset";
        private const float AgentAcceleration = 25f;
        private const float AgentAngularSpeed = 540f;
        private const float FallbackMoveSpeed = 1.5f;

        //(블렌드 트리로 바꿀 이동 상태, 속도 0일 때 쓸 대기 상태)
        private static readonly (string moveState, string idleState)[] LocomotionStates =
        {
            ("RUN", "IDLE"),
            ("ATTACKRUN", "MELEE_ATTACKIDLE"),
        };

        [MenuItem("Tools/NKT/Enemy Root Motion Setup")]
        private static void Setup()
        {
            AbstractEnemy[] enemies = Object.FindObjectsByType<AbstractEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (enemies.Length == 0)
            {
                Debug.LogWarning("[RootMotionSetup] 열린 씬에 AbstractEnemy가 없습니다.");
                return;
            }

            Animator firstAnimator = enemies[0].GetComponentInChildren<Animator>(true);
            AnimatorController controller = firstAnimator != null
                ? firstAnimator.runtimeAnimatorController as AnimatorController
                : null;
            if (controller == null)
            {
                Debug.LogError("[RootMotionSetup] 적의 Animator Controller를 찾을 수 없습니다.");
                return;
            }

            AnimParamSO speedParam = GetOrCreateSpeedParam();
            float agentSpeed = SetupController(controller);

            foreach (AbstractEnemy enemy in enemies)
                SetupEnemy(enemy, speedParam, agentSpeed);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[RootMotionSetup] 완료: 적 {enemies.Length}개, 에이전트 Speed {agentSpeed:0.00}. 씬을 저장하세요.");
        }

        private static AnimParamSO GetOrCreateSpeedParam()
        {
            AnimParamSO param = AssetDatabase.LoadAssetAtPath<AnimParamSO>(SpeedParamAssetPath);
            if (param == null)
            {
                param = ScriptableObject.CreateInstance<AnimParamSO>();
                AssetDatabase.CreateAsset(param, SpeedParamAssetPath);
            }

            SerializedObject so = new SerializedObject(param);
            so.FindProperty("<ParamName>k__BackingField").stringValue = SpeedParamName;
            so.FindProperty("<ParamHash>k__BackingField").intValue = Animator.StringToHash(SpeedParamName);
            so.ApplyModifiedPropertiesWithoutUndo();
            return param;
        }

        //이동 상태를 (대기 -> 이동) 블렌드 트리로 바꾸고, 가장 빠른 이동 클립 속도를 반환한다.
        private static float SetupController(AnimatorController controller)
        {
            if (controller.parameters.All(p => p.name != SpeedParamName))
                controller.AddParameter(SpeedParamName, AnimatorControllerParameterType.Float);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            float maxMoveSpeed = 0f;

            foreach ((string moveName, string idleName) in LocomotionStates)
            {
                AnimatorState moveState = FindState(stateMachine, moveName);
                AnimatorState idleState = FindState(stateMachine, idleName);
                if (moveState == null || idleState == null)
                {
                    Debug.LogWarning($"[RootMotionSetup] {moveName} 또는 {idleName} 상태가 없어 건너뜁니다.");
                    continue;
                }

                AnimationClip moveClip = GetMoveClip(moveState.motion);
                AnimationClip idleClip = idleState.motion as AnimationClip;
                if (moveClip == null || idleClip == null)
                {
                    Debug.LogWarning($"[RootMotionSetup] {moveName}/{idleName}의 클립을 찾을 수 없어 건너뜁니다.");
                    continue;
                }

                moveClip = ConfigureRootMotionImport(moveClip);

                float moveSpeed = moveClip.averageSpeed.magnitude;
                if (moveSpeed < 0.1f)
                {
                    Debug.LogWarning($"[RootMotionSetup] {moveClip.name}에 앞으로 가는 이동량이 거의 없습니다(In Place 클립?). 기본값 {FallbackMoveSpeed}를 사용합니다.");
                    moveSpeed = FallbackMoveSpeed;
                }

                BlendTree tree = new BlendTree
                {
                    name = $"{moveName} Blend",
                    blendParameter = SpeedParamName,
                    blendType = BlendTreeType.Simple1D,
                    useAutomaticThresholds = false,
                    hideFlags = HideFlags.HideInHierarchy
                };
                AssetDatabase.AddObjectToAsset(tree, controller);
                tree.AddChild(idleClip, 0f);
                tree.AddChild(moveClip, moveSpeed);

                if (moveState.motion is BlendTree oldTree && AssetDatabase.IsSubAsset(oldTree))
                    Object.DestroyImmediate(oldTree, true); //다시 실행했을 때 이전 트리 정리

                moveState.motion = tree;
                maxMoveSpeed = Mathf.Max(maxMoveSpeed, moveSpeed);
                Debug.Log($"[RootMotionSetup] {moveName}: {idleClip.name}(0) -> {moveClip.name}({moveSpeed:0.00})");
            }

            EditorUtility.SetDirty(controller);
            return maxMoveSpeed > 0f ? maxMoveSpeed : FallbackMoveSpeed;
        }

        private static AnimationClip GetMoveClip(Motion motion)
        {
            if (motion is AnimationClip clip) return clip;
            if (motion is BlendTree tree && tree.children.Length > 0)
                return tree.children[^1].motion as AnimationClip; //이미 블렌드 트리면 마지막(가장 빠른) 클립
            return null;
        }

        //회전과 높이는 포즈에 굽고, 앞뒤 이동(XZ)만 루트모션으로 남긴다.
        private static AnimationClip ConfigureRootMotionImport(AnimationClip clip)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogWarning($"[RootMotionSetup] {path}는 FBX가 아니라 임포트 설정을 바꾸지 않습니다.");
                return clip;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips.Length == 0)
                clips = importer.defaultClipAnimations;

            bool isFound = false;
            foreach (ModelImporterClipAnimation clipAnimation in clips)
            {
                if (clipAnimation.name != clip.name) continue;

                clipAnimation.loopTime = true;
                clipAnimation.lockRootRotation = true;      //Rotation: Bake Into Pose
                clipAnimation.lockRootHeightY = true;       //Position Y: Bake Into Pose
                clipAnimation.keepOriginalPositionY = false;
                clipAnimation.heightFromFeet = true;        //Position Y: Based Upon Feet
                clipAnimation.lockRootPositionXZ = false;   //Position XZ: 루트모션으로 사용
                isFound = true;
            }

            if (!isFound)
            {
                Debug.LogWarning($"[RootMotionSetup] {path}에서 {clip.name} 클립 설정을 찾지 못했습니다.");
                return clip;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            //재임포트 후 클립을 다시 불러와야 바뀐 이동 속도가 반영된다.
            return AssetDatabase.LoadAllAssetsAtPath(path)
                       .OfType<AnimationClip>()
                       .FirstOrDefault(a => a.name == clip.name) ?? clip;
        }

        private static void SetupEnemy(AbstractEnemy enemy, AnimParamSO speedParam, float agentSpeed)
        {
            Animator animator = enemy.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[RootMotionSetup] {enemy.name}에 Animator가 없습니다.", enemy);
                return;
            }

            GameObject visual = animator.gameObject;

            EnemyNavRenderer navRenderer = visual.GetComponent<EnemyNavRenderer>();
            if (navRenderer == null)
                navRenderer = Undo.AddComponent<EnemyNavRenderer>(visual);

            SerializedObject so = new SerializedObject(navRenderer);
            so.FindProperty("speedParam").objectReferenceValue = speedParam;
            so.ApplyModifiedProperties();

            //IRenderer가 둘이면 GetModule<IRenderer>()가 모호해지므로 기존 AgentRenderer는 제거
            List<AgentRenderer> oldRenderers = visual.GetComponents<AgentRenderer>()
                .Where(r => r.GetType() == typeof(AgentRenderer))
                .ToList();
            foreach (AgentRenderer oldRenderer in oldRenderers)
                Undo.DestroyObjectImmediate(oldRenderer);

            Undo.RecordObject(animator, "Enable root motion");
            animator.applyRootMotion = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (enemy.TryGetComponent(out NavMeshAgent agent))
            {
                Undo.RecordObject(agent, "Setup nav agent");
                agent.speed = agentSpeed;
                agent.acceleration = AgentAcceleration;
                agent.angularSpeed = AgentAngularSpeed;
                PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
            }

            Debug.Log($"[RootMotionSetup] {enemy.name}: 렌더러 교체, 루트모션 켬, 에이전트 설정 완료", enemy);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state.name == stateName)
                    return child.state;
            }

            foreach (ChildAnimatorStateMachine sub in stateMachine.stateMachines)
            {
                AnimatorState found = FindState(sub.stateMachine, stateName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
