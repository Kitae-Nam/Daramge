using System.IO;
using System.Linq;
using CHG._02_Scripts.CoreSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.Agent.FSM.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;

        private Button _folderBtn;
        private Button _generateBtn;
        private Label _folderPathLabel;

        private string _folderPath;
        private StateListSO _targetData;
        
        public override VisualElement CreateInspectorGUI()
        {
            _targetData = (StateListSO)target;
            
            VisualElement root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            editorView.CloneTree(root);

            _folderBtn = root.Q<Button>("FolderBtn");
            _generateBtn = root.Q<Button>("GenerateBtn");
            _folderPathLabel = root.Q<Label>("SelectedFolderLabel");
            _folderPathLabel.text = "No folder selected";

            _folderBtn.clicked += HandleFolderBtnClick;
            _generateBtn.clicked += HandleCodeGenerateClick;

            if (_targetData != null && !string.IsNullOrEmpty(_targetData.generatePath))
            {
                _folderPath = _targetData.generatePath;
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_targetData.generatePath);
            }
            
            return root;
        }

        private void HandleCodeGenerateClick()
        {
            if (string.IsNullOrEmpty(_folderPath) || !Directory.Exists(_folderPath))
            {
                EditorUtility.DisplayDialog("Folder doesn't exist", $"폴더 : {_folderPath}", "Ok");
                return;
            }

            int index = 0;
            string enumCode = string.Join(",", _targetData.states.Select(so =>
            {
                so.assetIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.stateName} = {index++}";
            }));
            
            string ns = FileUtil.GetProjectRelativePath(_folderPath).Substring("Assets/".Length);
            if (ns.StartsWith("Scripts/"))
            {
                ns = ns.Substring("Scripts/".Length); //이것도 잘라.
            }
            ns = ns.Replace("/", ".");  //슬래시를 점으로 변경해서 이름공간으로 변경
            
            string finalCode = string.Format(CodeFormat.EnumFormat, ns, _targetData.enumName, enumCode);
            File.WriteAllText($"{_folderPath}/{_targetData.enumName}.cs", finalCode);
            
            AssetDatabase.SaveAssets(); //Dirty처리된 모든 것을 저장한다.
            AssetDatabase.Refresh(); //저장후에 컴파일까지 한다.
        }

        private void HandleFolderBtnClick()
        {
            _folderPath = EditorUtility.OpenFolderPanel("Select folder", _folderPath, "");

            if (!string.IsNullOrEmpty(_folderPath))
            {
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_folderPath);
                _targetData.generatePath = _folderPath;
                EditorUtility.SetDirty(_targetData);
            }
            
            AssetDatabase.SaveAssetIfDirty(_targetData);
        }
    }
}