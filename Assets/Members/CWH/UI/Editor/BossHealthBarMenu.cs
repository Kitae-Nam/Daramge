using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Daramge.BossUI.Editor
{
    /// <summary>사용자가 메뉴를 실행할 때만 HUD를 생성한다. 런타임 코드에 에디터 의존성이 섞이지 않게 분리한다.</summary>
    public static class BossHealthBarMenu
    {
        private const string UiPath = "Assets/Members/CWH/UI/";

        [MenuItem("GameObject/CWH/Boss Health Bar (UI Toolkit)", false, 10)]
        private static void Create()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "BossHealthBar UI.uxml");
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "Default Panel Settings.asset");
            if (tree == null || panel == null)
            {
                Debug.LogError("CWH/UI의 UXML 또는 Panel Settings를 찾을 수 없습니다.");
                return;
            }

            BossHealth boss = Selection.activeGameObject == null
                ? null : Selection.activeGameObject.GetComponentInParent<BossHealth>();
            // 새 오브젝트만 생성해 기존 씬의 작업 중인 UI나 보스 설정을 덮어쓰지 않는다.
            var target = new GameObject("Boss Health Bar (UI Toolkit)");
            Undo.RegisterCreatedObjectUndo(target, "Create Boss Health Bar");
            // 프로젝트의 Unity 6.6 런타임 UI 구성에 맞춰 PanelRenderer를 사용한다.
            PanelRenderer panelRenderer = target.AddComponent<PanelRenderer>();
            panelRenderer.panelSettings = panel;
            panelRenderer.visualTreeAsset = tree;
            BossHealthBarController controller = target.AddComponent<BossHealthBarController>();
            controller.Bind(boss, boss == null ? "THE KING" : boss.name, null, 3);
            Selection.activeGameObject = target;
            EditorUtility.SetDirty(controller);
        }
    }
}
