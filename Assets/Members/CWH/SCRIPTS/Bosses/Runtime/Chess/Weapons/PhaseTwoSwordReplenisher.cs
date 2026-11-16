using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>전환 정리가 끝난 시점에 한 번만 부족한 검을 생성한다. 전투 중 파괴는 보충하지 않는다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossSwordInventory))]
    public sealed class PhaseTwoSwordReplenisher : MonoBehaviour
    {
        [SerializeField] private BossSword _swordPrefab;
        [SerializeField] private Transform _swordParent;
        [SerializeField, Min(1)] private int _targetCount = 6;
        private bool _hasReplenished;

        public void ReplenishOnce()
        {
            if (_hasReplenished) return;
            if (_swordPrefab == null)
            {
                Debug.LogError("2페이즈 보충용 검 프리팹을 연결해 주세요.", this);
                return;
            }

            var inventory = GetComponent<BossSwordInventory>();
            inventory.RemoveDestroyed();
            int missing = Mathf.Max(0, _targetCount - inventory.LivingCount);
            Transform parent = _swordParent != null ? _swordParent : transform;
            for (int i = 0; i < missing; i++)
            {
                BossSword sword = Instantiate(_swordPrefab, parent, false);
                sword.name = "Phase two orbit sword";
                inventory.Register(sword);
            }
            _hasReplenished = true;
        }
    }
}
