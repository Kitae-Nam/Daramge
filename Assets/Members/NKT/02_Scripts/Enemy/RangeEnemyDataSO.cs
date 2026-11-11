using UnityEngine;

namespace NKT.Enemy
{
    public enum RangeField
    {
        SprintDistance,
        MaxRange,
        MinRange,
        RunOutDistance,
        PreferDistance
    }
    [CreateAssetMenu(fileName = "Range Enemy data", menuName = "Enemy/Range Enemy data", order = 0)]
    public class RangeEnemyDataSO : ScriptableObject
    {
        [Tooltip("스프린트 거리는 맥스 값보다 3정도 크게 설정해야합니다.")]
        [field: SerializeField] public float SprintDistance { get; private set; }
        [field: SerializeField] public float MaxRange { get; private set; }
        [field: SerializeField] public float MinRange { get; private set; }
        [field: SerializeField] public float RunOutDistance { get; private set; }
        [field: SerializeField] public float PreferDistance { get; private set; }
        
        public float GetRandomRange() => Random.Range(MinRange, MaxRange);

        public float GetFieldValue(RangeField field) => field switch
        {
            RangeField.SprintDistance => SprintDistance,
            RangeField.MaxRange => MaxRange,
            RangeField.MinRange => MinRange,
            RangeField.RunOutDistance => RunOutDistance,
            RangeField.PreferDistance => PreferDistance,
            _ => 0f
        };
    }
}