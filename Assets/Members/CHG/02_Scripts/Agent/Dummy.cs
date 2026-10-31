using System.Collections;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using UnityEngine;

namespace CHG._02_Scripts.Agent
{
    public class Dummy : Agent
    {
        [SerializeField] private AnimParamSO idleParam;
        [SerializeField] private AnimParamSO hitParam;
        [SerializeField] private AnimParamSO deathParam;
        [SerializeField] private float respawnDelay = 2f;

        private IRenderer _renderer;
        private Collider[] _colliders;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _renderer = GetModule<IRenderer>();
            _colliders = GetComponents<Collider>();
            Debug.Assert(_renderer != null, $"renderer is null : {gameObject.name}");
            Debug.Assert(Health != null, $"health is null : {gameObject.name}");
        }

        protected override void HandleHitEvent()
        {
            if (IsDead) return;
            _renderer.PlayClip(hitParam.ParamHash, 0f, 0.05f);
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            SetCollidersEnabled(false);
            _renderer.PlayClip(deathParam.ParamHash, 0f, 0.05f);
            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            Health.ResetHealth();
            IsDead = false;
            SetCollidersEnabled(true);
            _renderer.PlayClip(idleParam.ParamHash, 0f, 0.05f);
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            foreach (Collider bodyCollider in _colliders)
                bodyCollider.enabled = isEnabled;
        }
    }
}