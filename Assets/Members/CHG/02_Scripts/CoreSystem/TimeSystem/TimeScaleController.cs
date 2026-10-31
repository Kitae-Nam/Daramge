using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.TimeSystem
{
    public class TimeScaleController : MonoBehaviour
    {
        private const float DEFAULT_TIME_SCALE = 1f;
        
        [SerializeField] private bool scaleFixedDeltaTime = true;

        private readonly Dictionary<object, float> _requests = new Dictionary<object, float>();
        private float _defaultFixedDeltaTime = Time.fixedDeltaTime;
        
        private void Awake()
            => _defaultFixedDeltaTime = Time.fixedDeltaTime;

        private void OnEnable()
        {
            EventBus<TimeScaleRequestEvent>.Subscribe(HandleRequest);
            EventBus<TimeScaleReleaseEvent>.Subscribe(HandleRelease);
        }

        private void OnDisable()
        {
            EventBus<TimeScaleRequestEvent>.Unsubscribe(HandleRequest);
            EventBus<TimeScaleReleaseEvent>.Unsubscribe(HandleRelease);
            _requests.Clear();
            ApplyTimeScale();
        }

        private void HandleRequest(TimeScaleRequestEvent evt)
        {
            Debug.Assert(evt.Requester != null, $"time scale requester is null : {gameObject.name}");
            if (evt.Requester == null) return;

            _requests[evt.Requester] = Mathf.Clamp(evt.TimeScale, 0f, DEFAULT_TIME_SCALE);
            ApplyTimeScale();   
        }

        private void HandleRelease(TimeScaleReleaseEvent evt)
        {
            if (evt.Requester != null && _requests.Remove(evt.Requester))
                ApplyTimeScale();
        }
        
        private void ApplyTimeScale()
        {
            float timeScale = DEFAULT_TIME_SCALE;
            foreach (float requestScale in _requests.Values)
                timeScale = Mathf.Min(timeScale, requestScale);

            Time.timeScale = timeScale;
            if (scaleFixedDeltaTime && timeScale > 0f)
                Time.fixedDeltaTime = _defaultFixedDeltaTime * timeScale;
        }
    }
}