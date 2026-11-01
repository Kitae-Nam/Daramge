using System;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.CoreSystem.TimeSystem;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.FeedbackSystem
{
    public class HitStopController : MonoBehaviour 
    {
        private bool _isStopping;
        private float _stopEndTime;
        private float _stopTimeScale;
        private void OnEnable() => EventBus<HitStopEvent>.Subscribe(HandleHitStop);

        private void OnDisable()
        {
            EventBus<HitStopEvent>.Unsubscribe(HandleHitStop);
            EndHitStop();
        }

        private void Start()
            => Debug.Assert(EventBus<TimeScaleRequestEvent>.ListenerCount > 0,
                $"TimeScaleController is not in scene : {gameObject.name}");
        
        private void Update()
        {
            if (_isStopping && Time.unscaledTime >= _stopEndTime)
                EndHitStop();
        }
        
        private void HandleHitStop(HitStopEvent evt)
        {
            _stopEndTime = Mathf.Max(_stopEndTime, Time.unscaledTime + evt.Duration);
            _stopTimeScale = _isStopping ? Mathf.Min(_stopTimeScale, evt.TimeScale) : evt.TimeScale;
            _isStopping = true;
            EventBus<TimeScaleRequestEvent>.Raise(new TimeScaleRequestEvent(this, _stopTimeScale));
        }

        private void EndHitStop()
        {
            if (!_isStopping) return;
            _isStopping = false;
            EventBus<TimeScaleReleaseEvent>.Raise(new TimeScaleReleaseEvent(this));
        }
    }
}