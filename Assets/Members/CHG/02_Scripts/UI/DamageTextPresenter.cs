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

        private void OnEnable() => EventBus<DamageTakenEvent>.Subscribe(HandleDamageTaken);
        private void OnDisable() => EventBus<DamageTakenEvent>.Unsubscribe(HandleDamageTaken);

        private void HandleDamageTaken(DamageTakenEvent evt)
        {
            if (evt.Target is PlayerController) return;

            Vector3 position = evt.Target.transform.position + Vector3.up * heightOffset;
            string text = Mathf.RoundToInt(evt.Amount).ToString();
            MMFloatingTextSpawnEvent.Trigger(null, position, text, Vector3.zero, 1f, useUnscaledTime: true);
        }
    }
}