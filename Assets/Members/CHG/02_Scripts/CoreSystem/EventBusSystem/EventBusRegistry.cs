using System;
using System.Collections.Generic;
using UnityEngine;

namespace CHG._02_Scripts.CoreSystem.EventBusSystem
{
    public static class EventBusRegistry
    {
        static readonly List<Action> clearActions = new();
 
        internal static void Register(Action clear) => clearActions.Add(clear);
 
        public static void ClearAll()
        {
            foreach (var clear in clearActions) clear();
        }
 
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => ClearAll();
    }
}