using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.CoreSystem.LetterboxSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public class LetterboxPresenter : IDisposable
    {
        private const string VisibleClass = "letterbox--visible";

        private readonly VisualElement _letterbox;
        private readonly Dictionary<object, LetterboxSetting> _requests = new();

        public LetterboxPresenter(VisualElement letterbox)
        {
            _letterbox = letterbox;
            EventBus<LetterboxRequestEvent>.Subscribe(HandleRequest);
            EventBus<LetterboxReleaseEvent>.Subscribe(HandleRelease);
        }

        public void Dispose()
        {
            EventBus<LetterboxRequestEvent>.Unsubscribe(HandleRequest);
            EventBus<LetterboxReleaseEvent>.Unsubscribe(HandleRelease);
        }

        private void HandleRequest(LetterboxRequestEvent evt)
        {
            Debug.Assert(evt.Requester != null, "letterbox requester is null");
            if (evt.Requester == null) return;

            _requests[evt.Requester] = evt.Settings;
            Apply();
        }

        private void HandleRelease(LetterboxReleaseEvent evt)
        {
            if (evt.Requester != null && _requests.Remove(evt.Requester))
                Apply();
        }

        private void Apply()
        {
            _letterbox.EnableInClassList(VisibleClass, _requests.Count > 0);
            if (_requests.Count == 0) return;

            float heightPercent = 0f;
            float opacity = 0f;
            foreach (LetterboxSetting settings in _requests.Values)
            {
                heightPercent = Mathf.Max(heightPercent, settings.HeightPercent);
                opacity = Mathf.Max(opacity, settings.Opacity);
            }

            foreach (VisualElement bar in _letterbox.Children())
            {
                bar.style.height = Length.Percent(heightPercent);
                bar.style.backgroundColor = new Color(0f, 0f, 0f, opacity);
            }
        }
    }
}