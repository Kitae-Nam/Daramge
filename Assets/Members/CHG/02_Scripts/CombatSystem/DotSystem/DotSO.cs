using CHG._02_Scripts.CoreSystem;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.DotSystem
{
    [CreateAssetMenu(fileName = "Dot data", menuName = "Agent/Dot data", order = 0)]
    public class DotSO : ScriptableObject
    {
        [field: SerializeField] public float Duration { get; private set; } = 3f;
        [field: SerializeField] public float TickInterval { get; private set; } = 0.5f;
        [field: SerializeField] public float TickDamage { get; private set; } = 1f;
        [field: SerializeField] public int MaxStack { get; private set; } = 5; 
        [field: SerializeField] public DamageTextStyle TextStyle { get; private set; }
        [field: SerializeField] public PoolItemSO VfxItem { get; private set; } 
    }
}