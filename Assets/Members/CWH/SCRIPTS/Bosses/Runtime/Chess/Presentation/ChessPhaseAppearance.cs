using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>페이즈 이벤트를 받아 외형만 바꾼다. 공격 설정이나 전투 잠금을 변경하지 않는다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossPhaseController))]
    public sealed class ChessPhaseAppearance : MonoBehaviour
    {
        [SerializeField] private MeshFilter _bodyMesh;
        [SerializeField] private Mesh _blackKingMesh;
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Material _blackKingMaterial;
        private BossPhaseController _phase;
        private void Awake() => _phase = GetComponent<BossPhaseController>();
        private void OnEnable() { _phase.PhaseChanged += Apply; Apply(_phase.CurrentPhase); }
        private void OnDisable() { if (_phase != null) _phase.PhaseChanged -= Apply; }
        private void Apply(int phase)
        {
            if (phase != 2) return;
            if (_bodyMesh != null && _blackKingMesh != null) _bodyMesh.sharedMesh = _blackKingMesh;
            if (_bodyRenderer != null && _blackKingMaterial != null) _bodyRenderer.sharedMaterial = _blackKingMaterial;
        }
    }
}
