using UnityEngine;

namespace NKT.Enemy
{
    public enum EnemyDataField
    {
        DetectRadius,
        ViewAngle,
        StopDistance,
    }
    
    [CreateAssetMenu(fileName = "Enemy data", menuName = "Enemy/Enemy data")]
    public class EnemyDataSO : ScriptableObject
    {
        [field: SerializeField] public float DetectRadius { get; private set; } = 5f;
        [field: SerializeField] public float ViewAngle { get; private set; } = 160f;
        [field: SerializeField] public float StopDistance { get; private set; } = 1f;

        public float GetFieldValue(EnemyDataField fieldEnum) => fieldEnum switch
        {
            EnemyDataField.DetectRadius => DetectRadius,
            EnemyDataField.ViewAngle => ViewAngle,
            EnemyDataField.StopDistance => StopDistance,
            _ => 0
        };
    }
}