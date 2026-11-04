namespace CHG._02_Scripts.CombatSystem
{
    using UnityEngine;                
    using UnityEngine.Events;         

    namespace CHG._02_Scripts.CombatSystem   
    {
        public class HitReceiver : MonoBehaviour, IDamageable   
        {
            [field: SerializeField] public UnityEvent OnHit { get; private set; }   

            public void ApplyDamage(DamageData damageData) => OnHit?.Invoke();       
        }
    }
}