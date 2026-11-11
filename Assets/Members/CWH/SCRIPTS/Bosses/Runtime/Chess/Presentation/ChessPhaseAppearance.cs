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
        [Tooltip("전환 컷신이 있는 씬에서만 켠다. 외형 변경 시점은 Timeline Signal이 결정한다.")]
        [SerializeField] private bool _deferToCutscene;
        private BossPhaseController _phase;
        private void Awake() => _phase = GetComponent<BossPhaseController>();
        private void OnEnable() { _phase.PhaseChanged += Apply; Apply(_phase.CurrentPhase); }
        private void OnDisable() { if (_phase != null) _phase.PhaseChanged -= Apply; }
        private void Apply(int phase)
        {
            if (phase != 2 || _deferToCutscene) return;
            RevealPhaseTwo();
        }

        /// <summary>페이즈 판정과 외형 교체를 분리해 폭발 프레임에 검은 킹을 공개할 수 있게 한다.</summary>
        public void RevealPhaseTwo()
        {
            // Timeline 미리보기로 씬의 원본 Mesh/Material이 바뀌는 것을 막는다.
            if (!Application.isPlaying || _phase.CurrentPhase != 2) return;
            if (_bodyMesh != null && _blackKingMesh != null) _bodyMesh.sharedMesh = _blackKingMesh;
            if (_bodyRenderer != null && _blackKingMaterial != null) _bodyRenderer.sharedMaterial = _blackKingMaterial;
        }
    }
}
