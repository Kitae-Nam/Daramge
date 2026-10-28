using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Demo
{
    /// <summary>테스트 경기장에 저장된 NavMesh를 에이전트 생성 전에 등록하고 씬 종료 시 해제한다.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class BossArenaNavMesh : MonoBehaviour
    {
        [SerializeField] private NavMeshData _data;
        private NavMeshDataInstance _instance;
        private void OnEnable() { if (_data != null) _instance = NavMesh.AddNavMeshData(_data); }
        private void OnDisable() { if (_instance.valid) _instance.Remove(); }
    }
}
