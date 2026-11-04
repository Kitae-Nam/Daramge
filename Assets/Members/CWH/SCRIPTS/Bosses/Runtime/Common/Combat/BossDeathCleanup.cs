using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat
{
    [RequireComponent(typeof(BossHealth))]
    public sealed class BossDeathCleanup : MonoBehaviour
    {
        private BossHealth _health;

        private void Awake()
        {
            _health = GetComponent<BossHealth>();
        }

        private void OnEnable()
        {
            _health.Died += HandleDeath;

            if (_health.IsDead)
                HandleDeath();
        }

        private void OnDisable()
        {
            _health.Died -= HandleDeath;
        }

        private void HandleDeath()
        {
            Destroy(gameObject);
        }
    }
}