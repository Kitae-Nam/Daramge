using System;
using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat
{
    /// <summary>컷신 등 전투 차단 사유를 합친다. 한 사유를 해제해도 다른 사유의 잠금은 유지한다.</summary>
    [DisallowMultipleComponent]
    public sealed class BossCombatGate : MonoBehaviour
    {
        [SerializeField] private bool _combatEnabled = true;
        private readonly HashSet<object> _blockers = new HashSet<object>();
        public bool IsOpen => _combatEnabled && _blockers.Count == 0;
        public event Action<bool> AvailabilityChanged;
        public void SetCombatEnabled(bool enabled)
        {
            bool before = IsOpen;
            _combatEnabled = enabled;
            PublishChange(before);
        }
        public void Block(object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            bool before = IsOpen;
            _blockers.Add(source);
            PublishChange(before);
        }
        public void Unblock(object source)
        {
            bool before = IsOpen;
            _blockers.Remove(source);
            PublishChange(before);
        }
        private void PublishChange(bool before) { if (before != IsOpen) AvailabilityChanged?.Invoke(IsOpen); }
    }
}
