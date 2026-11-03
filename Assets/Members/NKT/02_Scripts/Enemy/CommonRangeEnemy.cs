using UnityEngine;

namespace NKT.Enemy
{
    public class CommonRangeEnemy : AbstractEnemy, IHasRangeData
    {
        [field: SerializeField] public RangeEnemyDataSO RangeData { get; private set; }
    }
}