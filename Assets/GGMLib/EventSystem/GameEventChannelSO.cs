using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGMLib.EventSystem
{
    [CreateAssetMenu(fileName = "Event channel", menuName = "Lib/EventChannel", order = 10)]
    public class GameEventChannelSO : ScriptableObject
    {
        private Dictionary<Type, Action<GameEvent>> _events = new();
        private Dictionary<Delegate, Action<GameEvent>> _lookup = new();
        
        public void AddListener<T>(Action<T> handler) where T : GameEvent
        {
            if (_lookup.ContainsKey(handler)) return;
            
            Action<GameEvent> castHandler = (evt) => handler(evt as T);
            _lookup[handler] = castHandler;
            
            Type evtType = typeof(T);
            if (_events.ContainsKey(evtType))
            {
                _events[evtType] += castHandler;
            }
            else
            {
                _events[evtType] = castHandler;
            }
        }

        public void RemoveListener<T>(Action<T> handler) where T : GameEvent
        {
            Type evtType = typeof(T);
            if (_lookup.TryGetValue(handler, out Action<GameEvent> lookUpAction))
            {
                if (_events.TryGetValue(evtType, out Action<GameEvent> castHandler))
                {
                    castHandler -= lookUpAction;
                    if (castHandler == null)
                    {
                        _events.Remove(evtType);
                    }
                    else
                    {
                        _events[evtType] = castHandler;
                    }
                }
                
                _lookup.Remove(handler);
            }
        }

        public void RaiseEvent(GameEvent evt)
        {
            if (_events.TryGetValue(evt.GetType(), out Action<GameEvent> castHandler))
            {
                castHandler?.Invoke(evt);
            }
        }

        public void Clear()
        {
            _events.Clear();
            _lookup.Clear();
        }
        
    }
}