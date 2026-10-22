using System;
using System.Collections.Generic;
using UnityEngine;

namespace Members.CHG._02_Scripts.CoreSystem.EventBusSystem
{
    public static class EventBus<T> where T : struct, IEvent
    {
        static readonly List<Action<T>> listeners = new();
 
        static Action<T>[] snapshot = Array.Empty<Action<T>>();
        static bool dirty;
 
        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }
 
        public static void Subscribe(Action<T> listener)
        {
            if (listener == null || listeners.Contains(listener)) return;
            listeners.Add(listener);
            dirty = true;
        }
 
        public static void Unsubscribe(Action<T> listener)
        {
            if (listener == null) return;
            if (listeners.Remove(listener)) dirty = true;
        }
 
        public static void Raise(T evt)
        {
            if (dirty)
            {
                snapshot = listeners.ToArray();
                dirty = false;
            }
 
            foreach (var listener in snapshot)
            {
                try { listener(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
 
        public static void Clear()
        {
            listeners.Clear();
            snapshot = Array.Empty<Action<T>>();
            dirty = false;
        }
 
        public static int ListenerCount => listeners.Count;
    }
}