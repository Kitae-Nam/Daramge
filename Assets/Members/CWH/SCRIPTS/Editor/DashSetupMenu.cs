#if UNITY_EDITOR
using System.Linq;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using Memebers.CWH.SCRIPTS.TEST;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Members.CWH.SCRIPTS.Editor
{
    /// <summary>
    /// 열려 있는 씬의 사용자 편집을 유지한 채 기존 보스와 대쉬 경고를 연결한다.
    /// 자동 실행하지 않으며 Undo가 가능한 명시적인 메뉴로 제공한다.
    /// </summary>
    public static class DashSetupMenu
    {
        private const string MenuPath = "CWH/Boss/대쉬 연결 및 저장";

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            BossTarget[] bosses = Object.FindObjectsByType<BossTarget>(FindObjectsInactive.Exclude)
                .Where(value => value.gameObject.scene == EditorSceneManager.GetActiveScene()).ToArray();
            BossTarget boss = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<BossTarget>() : null;
            if (boss == null && bosses.Length == 1) boss = bosses[0];
            if (boss == null)
            {
                Debug.LogError("대쉬를 연결할 보스 루트(BossTarget이 있는 오브젝트)를 선택하세요.");
                return;
            }

            Renderer plane = boss.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(value => value.name == "DashTelegraphEffect");
            if (plane == null)
            {
                Debug.LogError("보스 자식에서 DashTelegraphEffect 경고 평면을 찾지 못했습니다.", boss);
                return;
            }

            Undo.SetCurrentGroupName("대쉬 패턴 연결");
            AddIfMissing<BossDashMotor>(boss.gameObject);
            AddIfMissing<DashHitbox>(boss.gameObject);
            bool isNewPattern = boss.GetComponent<DashPattern>() == null;
            DashPattern pattern = AddIfMissing<DashPattern>(boss.gameObject);
            BossTelegraphPresenter presenter = AddIfMissing<BossTelegraphPresenter>(plane.gameObject);
            DashTelegraphPresenter layout = AddIfMissing<DashTelegraphPresenter>(plane.gameObject);
            SetReference(presenter, "_targetRenderer", plane);
            SetReference(pattern, "_telegraph", layout);
            if (isNewPattern)
            {
                var serialized = new SerializedObject(pattern);
                serialized.FindProperty("_warningDuration").floatValue = 1.2f;
                serialized.FindProperty("_recoveryDuration").floatValue = 0.8f;
                serialized.ApplyModifiedProperties();
            }

            // 실제 플레이어가 아닌 기존 테스트 블록에만 검증용 체력을 붙인다.
            if (boss.HasTarget && boss.Target.GetComponent<TestCameraController>() != null &&
                boss.Target.GetComponent<IDamageReceiver>() == null)
                AddIfMissing<TestDamageReceiver>(boss.Target.gameObject);

            EditorSceneManager.MarkSceneDirty(boss.gameObject.scene);
            EditorSceneManager.SaveScene(boss.gameObject.scene);
            Selection.activeGameObject = boss.gameObject;
            Debug.Log("대쉬 연결 완료. 플레이 모드에서 Dash Pattern > 테스트/대쉬 패턴 요청을 실행하세요.", pattern);
        }

        private static T AddIfMissing<T>(GameObject owner) where T : Component
            => owner.TryGetComponent(out T component) ? component : Undo.AddComponent<T>(owner);

        private static void SetReference(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        [MenuItem(MenuPath, true)]
        private static bool CanSetup() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
#endif
