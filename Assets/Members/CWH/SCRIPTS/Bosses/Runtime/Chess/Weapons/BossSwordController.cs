using System.Collections.Generic;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>필드에 있는 보스 검과 파괴 보상을 관리한다. 패턴 후딜이 끝나도 박힌 검은 유지한다.</summary>
    [RequireComponent(typeof(BossHealth), typeof(BossGroggyController))]
    public sealed class BossSwordController : MonoBehaviour
    {
        [SerializeField] private ThrownBossSword _swordPrefab;
        [SerializeField, Min(0f)] private float _breakBonusDamage = 100f;

        [Header("투척 준비 연출")] [SerializeField] private Transform _throwPreparationCenter;

        [SerializeField, Min(0f)] private float _throwPreparationExtraDistance = 1f;
        [SerializeField, Range(.01f, 1f)] private float _throwPreparationPortion = 0.35f;

        private BossHealth _health;
        private BossGroggyController _groggy;
        private BossCombatPresentation _presentation;
        private BossCombatGate _combatGate;
        private BossSwordInventory _inventory;

        private readonly List<ThrownBossSword> _active = new List<ThrownBossSword>();
        private readonly List<BossSword> _preparedSwords = new List<BossSword>();
        public ThrownBossSword ActiveSword => _active.Count > 0 ? _active[0] : null;
        public bool HasSword => _active.Count > 0;

        public int AvailableCount =>
            _inventory != null ? _inventory.OrbitingCount : (!HasSword && _swordPrefab != null ? 1 : 0);

        public bool CanThrow => isActiveAndEnabled && !HasSword && AvailableCount > 0 && !_health.IsDead;

        public bool CanLaunchPrepared => isActiveAndEnabled &&
                                         !_health.IsDead &&
                                         !HasSword &&
                                         _preparedSwords.Count > 0;

        private void Awake()
        {
            _health = GetComponent<BossHealth>();
            _groggy = GetComponent<BossGroggyController>();
            _presentation = GetComponent<BossCombatPresentation>();
            _combatGate = GetComponent<BossCombatGate>();
            _inventory = GetComponent<BossSwordInventory>();
        }

        private void OnEnable()
        {
            _health.Died += Clear;
            _groggy.Entered += Clear;
            if (_combatGate != null) _combatGate.AvailabilityChanged += HandleCombatAvailability;
        }

        private void HandleCombatAvailability(bool open)
        {
            if (!open) Clear();
        }

        public ThrownBossSword Throw(Transform target, Vector3 destination, float radius)
        {
            if (!CanThrow) return null;
            return LaunchNext(target, destination, radius);
        }

        /// <summary>예고한 지점마다 남은 실제 검만 던진다. 부족한 검을 새로 생성하지 않는다.</summary>
        public ThrownBossSword[] ThrowVolley(Transform target, Vector3[] destinations, float radius)
        {
            if (!CanThrow) return new ThrownBossSword[0];
            var launched = new List<ThrownBossSword>();
            foreach (Vector3 destination in destinations)
            {
                ThrownBossSword sword = LaunchNext(target, destination, radius);
                if (sword == null) break;
                launched.Add(sword);
            }

            return launched.ToArray();
        }

        public int PrepareThrow(int cnt)
        {
            if (cnt <= 0 || _preparedSwords.Count > 0 ||
                _inventory == null || !CanThrow)
            {
                return 0;
            }

            if (_throwPreparationCenter == null)
            {
                Debug.LogError("투척 준비의 공전 중심을 연결해 주세요.", this);
                return 0;
            }

            for (int i = 0; i < cnt; i++)
            {
                if (!_inventory.TryPrepareThrowSword(out BossSword sword))
                    break;

                if (!sword.PoseBinding.TryCapture())
                {
                    sword.TryCancelThrowPrepare();
                    break;
                }

                sword.SwingTrail.StopAndClear();
                sword.transform.SetParent(null, true);

                SwordThrowPreparationMotion motion =
                    sword.GetComponent<SwordThrowPreparationMotion>();

                if (motion == null)
                    motion = sword.gameObject.AddComponent<SwordThrowPreparationMotion>();

                motion.Begin(
                    _throwPreparationCenter.position,
                    _throwPreparationExtraDistance);

                _preparedSwords.Add(sword);
            }

            return _preparedSwords.Count;
        }

        public void CancelThrowPrepared(bool deferParenting = false)
        {
            foreach (BossSword sword in _preparedSwords)
            {
                if (sword == null)
                    continue;

                sword.PoseBinding.Restore(deferParenting);
            }

            _preparedSwords.Clear();
        }

        public void UpdateThrowPreparation(float progress)
        {
            float motionProgress = Mathf.Clamp01(progress/ Mathf.Max(.01f, _throwPreparationPortion));

            foreach (BossSword sword in _preparedSwords)
            {
                if (sword == null || sword.State != BossSwordState.PreparingThrow)
                    continue;
                
                SwordThrowPreparationMotion motion = sword.GetComponent<SwordThrowPreparationMotion>();
                
                if(motion != null)
                    motion.SetProgress(motionProgress);
                    
            }
        }

    public ThrownBossSword[] LaunchPreparedVolley(
            Transform target, Vector3[] destinations, float radius)
        {
            if (!CanLaunchPrepared || target == null || destinations == null)
                return new ThrownBossSword[0];
            
            UpdateThrowPreparation(1f);
            var launched = new List<ThrownBossSword>();
            
            int count = Mathf.Min(_preparedSwords.Count, destinations.Length);

            for (int i = 0; i < count; i++)
            {
                BossSword physical = _preparedSwords[0];

                // 준비 중 검이 사라졌다면 나머지 예약도 정리한다.
                if (physical == null || !physical.isActiveAndEnabled)
                    break;

                if (!physical.TryLaunch())
                    break;

                // 발사한 검은 이후 _active 목록이 관리한다.
                _preparedSwords.RemoveAt(0);

                ThrownBossSword thrown = physical.GetComponent<ThrownBossSword>();
                if (thrown == null)
                    thrown = physical.gameObject.AddComponent<ThrownBossSword>();

                _active.Add(thrown);
                thrown.Finished += HandleFinished;
                thrown.Embedded += HandleEmbedded;

                thrown.Launch(
                    transform,
                    target,
                    physical.transform.position,
                    destinations[i],
                    radius);

                launched.Add(thrown);
            }

            CancelThrowPrepared();
            return launched.ToArray();
        }

        private ThrownBossSword LaunchNext(Transform target, Vector3 destination, float radius)
        {
            ThrownBossSword thrown;
            Vector3 start;
            if (_inventory != null)
            {
                if (!_inventory.TryLaunchSword(out BossSword physical)) return null;
                physical.SwingTrail.StopAndClear();
                thrown = physical.GetComponent<ThrownBossSword>();
                if (thrown == null) thrown = physical.gameObject.AddComponent<ThrownBossSword>();
                start = physical.transform.position;
            }
            else
            {
                // 공전 검이 없는 기존 테스트/프리팹만 지원하는 호환 경로다.
                if (_swordPrefab == null || HasSword) return null;
                thrown = Instantiate(_swordPrefab);
                start = transform.position + Vector3.up * 4f;
                if (_presentation != null) _presentation.SetWeaponVisible(false);
            }

            _active.Add(thrown);
            thrown.Finished += HandleFinished;
            thrown.Embedded += HandleEmbedded;
            thrown.Launch(transform, target, start, destination, radius);
            return thrown;
        }

        private void HandleEmbedded(ThrownBossSword sword)
        {
            if (!_active.Contains(sword)) return;
            _presentation?.PlaySwordEmbedded();
        }

        private void HandleFinished(ThrownBossSword sword, bool broken)
        {
            if (!_active.Remove(sword)) return;
            sword.Finished -= HandleFinished;
            sword.Embedded -= HandleEmbedded;
            if (_inventory == null && _presentation != null) _presentation.SetWeaponVisible(true);
            if (broken)
                _presentation?.PlaySwordBroken();
            if (!broken || _health.IsDead || !isActiveAndEnabled) return;
            // 보너스 피해를 먼저 주므로 이 보상 자체에 새 그로기의 1.5배가 소급 적용되지는 않는다.
            _health.ReceiveDamage(_breakBonusDamage);
            if (!_health.IsDead) _groggy.TryEnterGroggy();
        }

        public void Clear()
        {
            CancelThrowPrepared();
            RecallAll(false);
        }

        private void RecallAll(bool deferParenting)
        {
            // 콜백에서 목록이 바뀌므로 복사본을 순회한다. 파괴된 검은 회수로 재생하지 않는다.
            foreach (ThrownBossSword sword in _active.ToArray())
                if (sword != null)
                    sword.Recall(deferParenting);
            _active.Clear();
        }

        private void OnDisable()
        {
            _health.Died -= Clear;
            _groggy.Entered -= Clear;
            if (_combatGate != null) _combatGate.AvailabilityChanged -= HandleCombatAvailability;
            CancelThrowPrepared(true);
            RecallAll(true);
        }
    }
}