using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.CoreSystem.FadeSystem;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public class ScreenFadePresenter : IDisposable
    {
        private readonly VisualElement _fade;

        public ScreenFadePresenter(VisualElement fade)
        {
            _fade = fade;
            EventBus<ScreenFadeEvent>.Subscribe(HandleFade);
        }

        public void Dispose() => EventBus<ScreenFadeEvent>.Unsubscribe(HandleFade);

        private void HandleFade(ScreenFadeEvent evt)
        {
            _fade.style.transitionDuration = new List<TimeValue> { new(evt.Duration, TimeUnit.Second) };
            _fade.style.opacity = evt.Alpha;
        }
    }
}