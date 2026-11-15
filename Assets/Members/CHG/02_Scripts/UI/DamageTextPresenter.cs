using System;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace CHG._02_Scripts.UI
{
    public class DamageTextPresenter : MonoBehaviour
    {
        [SerializeField] private float heightOffset = 1.8f;
        [SerializeField] private string numberFormat = "0.#";
        [SerializeField] private Gradient criticalGradient;
        [SerializeField] private float criticalIntensity = 1.5f;

        private void OnEnable() => EventBus<DamageTakenEvent>.Subscribe(HandleDamageTaken);
        private void OnDisable() => EventBus<DamageTakenEvent>.Unsubscribe(HandleDamageTaken);

        private void HandleDamageTaken(DamageTakenEvent evt)
        {
            if (evt.Target is PlayerController) return;

            Vector3 position = evt.Target.transform.position + Vector3.up * heightOffset;
            string text = evt.Amount.ToString(numberFormat);

            if (evt.IsCritical) 
            {
                MMFloatingTextSpawnEvent.Trigger(null, position, text, Vector3.zero, criticalIntensity,
                    forceColor: true, animateColorGradient: criticalGradient, useUnscaledTime: true); 
                return; 
            }
            
            MMFloatingTextSpawnEvent.Trigger(null, position, text, Vector3.zero, 1f, useUnscaledTime: true);
        }
    }
}