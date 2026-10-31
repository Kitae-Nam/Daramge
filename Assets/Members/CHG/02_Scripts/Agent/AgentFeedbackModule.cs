using System;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.FeedbackSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Agent
{
    public class AgentFeedbackModule : MonoBehaviour, IModule
    {
        [SerializeField] private FeedbackPlayer hitFeedback;
        [SerializeField] private FeedbackPlayer deathFeedback;

        private Agent _agent;
        private ActionDataModule _actionData;
        
        public void Initialize(ModuleOwner owner)
        {
            _agent = owner as Agent;
            _actionData = owner.GetModule<ActionDataModule>();
            Debug.Assert(_agent != null, $"owner is not Agent : {gameObject.name}");
            Debug.Assert(_actionData != null, $"ActionDataModule is null : {gameObject.name}");
            
            _agent.OnHit.AddListener(HandleHit);
            _agent.OnDeath.AddListener(HandleDeath);
        }

        private void OnDestroy()
        {
            if (_agent == null) return;
            _agent.OnHit.RemoveListener(HandleHit);
            _agent.OnDeath.RemoveListener(HandleDeath);
        }

        private void HandleHit()
        {
            PlayFeedback(hitFeedback);
        }

        private void HandleDeath()
        {
            PlayFeedback(deathFeedback);
        }

        private void PlayFeedback(FeedbackPlayer feedbackPlayer)
        {
            if (feedbackPlayer == null) return;
            feedbackPlayer.PlayFeedback(CreateContext());
        }

        private FeedbackContext CreateContext()
        {
            Vector3 fallbackDirection = -_agent.transform.forward;
            Vector3 direction = _actionData.Attacker != null
                ? _agent.transform.position - _actionData.Attacker.transform.position
                : fallbackDirection;
            direction.y = 0;
            
            if (direction.sqrMagnitude < Mathf.Epsilon)
                direction = fallbackDirection;

            return new FeedbackContext(_actionData.HitPoint, direction.normalized);
        }
    }
}