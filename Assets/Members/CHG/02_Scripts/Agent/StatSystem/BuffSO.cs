using UnityEngine;

namespace CHG._02_Scripts.Agent.StatSystem
{
    [CreateAssetMenu(fileName = "Buff data", menuName = "Agent/Buff data", order = 0)]
    public class BuffSO : ScriptableObject
    {
        [field: SerializeField] public float Duration { get; private set; } = 5f;
        [field: SerializeField] public StatBonus[] Bonuses { get; private set; }
    }
}