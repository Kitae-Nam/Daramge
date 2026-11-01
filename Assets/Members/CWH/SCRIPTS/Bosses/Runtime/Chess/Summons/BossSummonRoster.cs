using System.Collections.Generic;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;
using UnityEngine.AI;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>살아 있는 기물 목록과 소환 보호 배율의 수명을 함께 관리한다.</summary>
    [RequireComponent(typeof(BossHealth))]
    public sealed class BossSummonRoster : MonoBehaviour
    {
        private readonly List<ChessMinion> _living = new List<ChessMinion>();
        private BossHealth _health;
        private BossCombatGate _combatGate;
        public int LivingCount => _living.Count;
        public bool IsAttackInProgress => _living.Exists(minion => minion != null && minion.IsAttacking);
        private void Awake() { _health = GetComponent<BossHealth>(); _combatGate = GetComponent<BossCombatGate>(); }
        private void OnEnable()
        {
            _health.Died += Clear;
            if (_combatGate != null) _combatGate.AvailabilityChanged += HandleCombatAvailability;
        }
        private void HandleCombatAvailability(bool open) { if (!open) Clear(); }
        public void Register(ChessMinion minion)
        {
            if (minion == null || _living.Contains(minion)) return;
            _living.Add(minion);
            minion.Removed += HandleRemoved;
            _health.SetDamageMultiplier(this, 0.5f);
        }
        private void HandleRemoved(ChessMinion minion)
        {
            minion.Removed -= HandleRemoved;
            _living.Remove(minion);
            // 마지막 한 마리까지 사라져야 보호를 해제한다. 그로기 배율은 건드리지 않는다.
            if (_living.Count == 0) _health.RemoveDamageMultiplier(this);
        }
        public void Clear()
        {
            while (_living.Count > 0)
            {
                ChessMinion minion = _living[_living.Count - 1];
                _living.RemoveAt(_living.Count - 1);
                if (minion == null) continue;
                minion.Removed -= HandleRemoved;
                minion.Despawn();
            }
            _health.RemoveDamageMultiplier(this);
        }
        /// <summary>같은 NavMesh 섬에서 서로 겹치지 않는 두 위치를 먼저 확보한다.</summary>
        public bool TryGetSpawnPoints(out Vector3[] points)
        {
            var found = new List<Vector3>();
            var path = new NavMeshPath();
            for (int i = 0; i < 16 && found.Count < 2; i++)
            {
                float radians = (i * 137.5f) * Mathf.Deg2Rad;
                Vector3 desired = transform.position + new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * 6f;
                if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                if (Vector3.Distance(hit.position, transform.position) < 3f ||
                    (found.Count > 0 && Vector3.Distance(found[0], hit.position) < 3f)) continue;
                found.Add(hit.position);
            }
            points = found.ToArray();
            return points.Length == 2;
        }
        private void OnDisable()
        {
            _health.Died -= Clear;
            if (_combatGate != null) _combatGate.AvailabilityChanged -= HandleCombatAvailability;
            Clear();
        }
    }
}
