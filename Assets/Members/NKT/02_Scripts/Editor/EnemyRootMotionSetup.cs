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
    //열려있는 씬의 적을 기준으로 Animator 블렌드 트리, 클립 임포트 설정, 렌더러 교체, 에이전트 값을 맞춘다.
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
            ("ATTACK_RUN", "ATTACK_IDLE"),
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

            AnimParamSO speedParam = GetOrCreateSpeedParam();

            //적 종류마다 컨트롤러가 다르므로 컨트롤러별로 한 번씩 처리하고, 그 속도를 해당 적들에게 적용한다.
            Dictionary<AnimatorController, float> speedByController = new Dictionary<AnimatorController, float>();
            int setupCount = 0;

            foreach (AbstractEnemy enemy in enemies)
            {
                Animator animator = enemy.GetComponentInChildren<Animator>(true);
                AnimatorController controller = animator != null
                    ? animator.runtimeAnimatorController as AnimatorController
                    : null;
                if (controller == null)
                {
                    Debug.LogWarning($"[RootMotionSetup] {enemy.name}의 Animator Controller를 찾을 수 없어 건너뜁니다.", enemy);
                    continue;
                }

                if (!speedByController.TryGetValue(controller, out float agentSpeed))
                {
                    Debug.Log($"[RootMotionSetup] 컨트롤러 처리: {controller.name}");
                    agentSpeed = SetupController(controller);
                    RemoveUnusedBlendTrees(controller);
                    speedByController.Add(controller, agentSpeed);
                }

                SetupEnemy(enemy, speedParam, agentSpeed);
                setupCount++;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[RootMotionSetup] 완료: 적 {setupCount}개, 컨트롤러 {speedByController.Count}개. 씬을 저장하세요.");
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

        //이동/대기 상태에 루트모션용으로 설정한 클립을 연결하고, 가장 빠른 이동 클립 속도를 반환한다.
        //Speed 파라미터는 EnemyNavRenderer가 매 프레임 넣으므로 컨트롤러에 남겨둔다(없으면 경고가 뜬다).
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

                //대기 클립은 제자리에서 반복, 이동 클립은 앞뒤 이동만 루트모션으로 남긴다.
                idleClip = ConfigureClip(idleClip, isMoveClip: false);
                moveClip = ConfigureClip(moveClip, isMoveClip: true);

                float moveSpeed = moveClip.averageSpeed.magnitude;
                if (moveSpeed < 0.1f)
                {
                    Debug.LogWarning($"[RootMotionSetup] {moveClip.name}에 앞으로 가는 이동량이 거의 없습니다(In Place 클립?). 기본값 {FallbackMoveSpeed}를 사용합니다.");
                    moveSpeed = FallbackMoveSpeed;
                }

                //대기<->이동 블렌드 트리는 클립 길이 차이 때문에 출발/정지 구간에서 걸음이 늘어지므로 쓰지 않는다.
                //정지와 이동은 BT가 상태(IDLE/RUN)를 직접 바꾸고, 크로스페이드로 이어준다.
                idleState.motion = idleClip; //재임포트된 클립으로 다시 연결
                moveState.motion = moveClip; //이전 블렌드 트리는 RemoveUnusedBlendTrees에서 정리된다.
                maxMoveSpeed = Mathf.Max(maxMoveSpeed, moveSpeed);
                Debug.Log($"[RootMotionSetup] {moveName}: {moveClip.name}({moveSpeed:0.00}), {idleName}: {idleClip.name}");
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

        //루프를 켜고, 회전과 높이는 포즈에 굽는다.
        //이동 클립은 앞뒤 이동(XZ)을 루트모션으로 남기고, 대기 클립은 XZ도 굽어서 제자리에 있게 한다.
        private static AnimationClip ConfigureClip(AnimationClip clip, bool isMoveClip)
        {
            string path = AssetDatabase.GetAssetPath(clip);

            if (AssetImporter.GetAtPath(path) is ModelImporter importer)
                return ConfigureModelClip(importer, path, clip, isMoveClip);

            //FBX가 아닌 .anim 파일
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = !isMoveClip;                 //Loop Pose
            settings.loopBlendOrientation = true;             //Rotation: Bake Into Pose
            settings.loopBlendPositionY = true;               //Position Y: Bake Into Pose
            settings.keepOriginalPositionY = false;
            settings.heightFromFeet = true;                   //Position Y: Based Upon Feet
            settings.loopBlendPositionXZ = !isMoveClip;       //Position XZ: 대기 클립만 Bake
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip ConfigureModelClip(ModelImporter importer, string path, AnimationClip clip, bool isMoveClip)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips.Length == 0)
                clips = importer.defaultClipAnimations;

            bool isFound = false;
            foreach (ModelImporterClipAnimation clipAnimation in clips)
            {
                if (clipAnimation.name != clip.name) continue;

                clipAnimation.loopTime = true;
                if (!isMoveClip) clipAnimation.loopPose = true;
                clipAnimation.lockRootRotation = true;           //Rotation: Bake Into Pose
                clipAnimation.lockRootHeightY = true;            //Position Y: Bake Into Pose
                clipAnimation.keepOriginalPositionY = false;
                clipAnimation.heightFromFeet = true;             //Position Y: Based Upon Feet
                clipAnimation.lockRootPositionXZ = !isMoveClip;  //Position XZ: 대기 클립만 Bake
                isFound = true;
            }

            if (!isFound)
            {
                Debug.LogWarning($"[RootMotionSetup] {path}에서 {clip.name} 클립 설정을 찾지 못했습니다.");
                return clip;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            //재임포트 후 클립을 다시 불러와야 바뀐 설정과 이동 속도가 반영된다.
            return AssetDatabase.LoadAllAssetsAtPath(path)
                       .OfType<AnimationClip>()
                       .FirstOrDefault(a => a.name == clip.name) ?? clip;
        }

        //어떤 상태에서도 쓰이지 않는 블렌드 트리(이전 실행에서 남은 것)를 지운다.
        private static void RemoveUnusedBlendTrees(AnimatorController controller)
        {
            HashSet<BlendTree> usedTrees = new HashSet<BlendTree>();
            foreach (AnimatorControllerLayer layer in controller.layers)
                CollectUsedTrees(layer.stateMachine, usedTrees);

            string controllerPath = AssetDatabase.GetAssetPath(controller);
            List<BlendTree> unusedTrees = AssetDatabase.LoadAllAssetsAtPath(controllerPath)
                .OfType<BlendTree>()
                .Where(tree => !usedTrees.Contains(tree))
                .ToList();

            foreach (BlendTree tree in unusedTrees)
                Object.DestroyImmediate(tree, true);

            if (unusedTrees.Count > 0)
                Debug.Log($"[RootMotionSetup] 사용하지 않는 블렌드 트리 {unusedTrees.Count}개 정리");
        }

        private static void CollectUsedTrees(AnimatorStateMachine stateMachine, HashSet<BlendTree> usedTrees)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
                CollectTree(child.state.motion, usedTrees);

            foreach (ChildAnimatorStateMachine sub in stateMachine.stateMachines)
                CollectUsedTrees(sub.stateMachine, usedTrees);
        }

        private static void CollectTree(Motion motion, HashSet<BlendTree> usedTrees)
        {
            if (motion is not BlendTree tree || !usedTrees.Add(tree)) return;

            foreach (ChildMotion child in tree.children)
                CollectTree(child.motion, usedTrees); //블렌드 트리 안의 블렌드 트리
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
