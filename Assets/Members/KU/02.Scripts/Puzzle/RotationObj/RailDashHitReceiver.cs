using CHG._02_Scripts.CombatSystem;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RailDashHitReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] private DashRotationPuzzle rail;

    public void ApplyDamage(DamageData damageData)
    {
        if (rail != null && damageData.Attacker != null && damageData.HitNormal.sqrMagnitude > 0f)
            rail.ReceiveDashHitAtPoint(damageData.Attacker.transform, damageData.HitPoint);
    }
}
