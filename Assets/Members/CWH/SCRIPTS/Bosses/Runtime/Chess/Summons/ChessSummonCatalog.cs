using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>소환 후보만 제공한다. 퀸 등 새 기물은 배열에 등록하며 소환 패턴의 코드를 수정하지 않는다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossPhaseController))]
    public sealed class ChessSummonCatalog : MonoBehaviour
    {
        [SerializeField] private ChessMinion[] _phaseTwoPrefabs = new ChessMinion[0];
        private BossPhaseController _phase;
        private void Awake() => _phase = GetComponent<BossPhaseController>();

        public ChessMinion Select(ChessMinion normalPawn, ChessMinion normalKnight)
        {
            if (_phase.CurrentPhase == 2)
            {
                int count = 0;
                foreach (ChessMinion prefab in _phaseTwoPrefabs) if (prefab != null) count++;
                if (count > 0)
                {
                    int chosen = Random.Range(0, count);
                    foreach (ChessMinion prefab in _phaseTwoPrefabs)
                        if (prefab != null && chosen-- == 0) return prefab;
                }
            }
            return Random.value < 0.5f ? normalPawn : normalKnight;
        }
    }
}
