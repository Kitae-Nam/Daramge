using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.LetterboxSystem
{
    public class LetterboxRequester : MonoBehaviour
    {
        [SerializeField] private LetterboxSetting settings;

        public void Show()
            => EventBus<LetterboxRequestEvent>.Raise(new LetterboxRequestEvent(this, settings));

        public void Hide()
            => EventBus<LetterboxReleaseEvent>.Raise(new LetterboxReleaseEvent(this));

        private void OnDisable() => Hide();
    }
}