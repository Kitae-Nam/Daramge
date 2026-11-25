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
        [SerializeField] private bool _showThrowFeedback;

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
        private object _sequenceOwner;
        private int _launchedSwordCount;
        private int _brokenSwordCount;
        private int _plannedSwordCount;
        private bool _swordRecalled;
        public ThrownBossSword ActiveSword => _active.Count > 0 ? _active[0] : null;
        public bool HasSword => _active.Count > 0;

        public int AvailableCount =>
            _inventory != null ? _inventory.OrbitingCount : (!HasSword && _swordPrefab != null ? 1 : 0);

        public bool CanThrow => isActiveAndEnabled && _sequenceOwner == null &&
                                !HasSword && AvailableCount > 0 && !_health.IsDead;

        public bool CanLaunchPrepared => isActiveAndEnabled &&
                                         !_health.IsDead &&
                                         (!HasSword || _sequenceOwner != null) &&
                                         _preparedSwords.Count > 0;

        /// <summary>새 패턴은 박힌 검이 없을 때만 시작하고 같은 연속 공격만 예외적으로 다음 검을 준비한다.</summary>
        public bool TryBeginThrowSequence(object owner, int plannedSwordCount = 0)
        {
            if (owner == null || !CanThrow || _preparedSwords.Count > 0) return false;
            _sequenceOwner = owner;
            _plannedSwordCount = plannedSwordCount;
            return true;
        }

        public bool OwnsThrowSequence(object owner) => owner != null && ReferenceEquals(_sequenceOwner, owner);

        public int PrepareNextThrow(object owner)
        {
            if (!OwnsThrowSequence(owner) || !isActiveAndEnabled || _health.IsDead) return 0;
            return PrepareThrowInternal(1);
        }

        public void EndThrowSequence(object owner)
        {
            if (!OwnsThrowSequence(owner)) return;
            CancelThrowPrepared();
            _sequenceOwner = null;
            _plannedSwordCount = 0;
            RefreshThrowFeedback();
            TryFinishThrowGroup();
        }

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
            _groggy.Entered += HandleGroggyEntered;
            if (_combatGate != null) _combatGate.AvailabilityChanged += HandleCombatAvailability;
        }

        private void HandleCombatAvailability(bool open)
        {
            if (!open) Clear();
        }

        private void HandleGroggyEntered() => CancelThrows(animateEmbedded: true);

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
            if (!CanThrow) return 0;
            return PrepareThrowInternal(cnt);
        }

        private int PrepareThrowInternal(int cnt)
        {
            if (cnt <= 0 || _preparedSwords.Count > 0 ||
                _inventory == null)
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

                LaunchTrackedSword(thrown, target, physical.transform.position, destinations[i], radius);

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

            LaunchTrackedSword(thrown, target, start, destination, radius);
            return thrown;
        }

        private void LaunchTrackedSword(ThrownBossSword thrown, Transform target, Vector3 start,
            Vector3 destination, float radius)
        {
            // 발사한 검만 집계해서 준비가 취소된 검은 그로기 조건에 포함하지 않는다.
            _launchedSwordCount++;
            _active.Add(thrown);
            thrown.Finished += HandleFinished;
            thrown.Embedded += HandleEmbedded;
            thrown.Launch(transform, target, start, destination, radius, _showThrowFeedback);
            RefreshThrowFeedback();
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
            {
                _brokenSwordCount++;
                if (_showThrowFeedback) _presentation?.PulseSwordBreak();
                _presentation?.PlaySwordBroken();
                // 각 검의 보너스 피해는 그로기 배율이 적용되기 전에 지급한다.
                if (!_health.IsDead && isActiveAndEnabled)
                    _health.ReceiveDamage(_breakBonusDamage);
            }
            else if (_launchedSwordCount > 0)
            {
                _swordRecalled = true;
            }

            RefreshThrowFeedback();
            TryFinishThrowGroup();
        }

        private void RefreshThrowFeedback()
        {
            if (!_showThrowFeedback || _presentation == null) return;
            // 연속 투척 중에는 이번 패턴의 예정 수량을 보여준다. 종료 후에는 실제 발사만 집계한다.
            int total = _launchedSwordCount == 0 ? 0 : Mathf.Max(_plannedSwordCount, _launchedSwordCount);
            _presentation.ShowSwordBreakProgress(_brokenSwordCount, total, _swordRecalled);
        }

        private void TryFinishThrowGroup()
        {
            // 다음 투척이 남았거나 아직 필드에 검이 있으면 한 패턴의 결과가 확정되지 않았다.
            if (_sequenceOwner != null || HasSword) return;

            // 회수된 검도 발사 수에 남으므로 모든 검의 파괴 여부를 구분할 수 있다.
            bool allBroken = _launchedSwordCount > 0 && _launchedSwordCount == _brokenSwordCount;
            _launchedSwordCount = _brokenSwordCount = 0;
            _swordRecalled = false;
            RefreshThrowFeedback();
            if (allBroken && isActiveAndEnabled && !_health.IsDead && (_combatGate == null || _combatGate.IsOpen))
                _groggy.TryEnterGroggy();
        }

        public void Clear() => CancelThrows();

        private void CancelThrows(bool deferParenting = false, bool animateEmbedded = false)
        {
            // 중단된 연속 투척은 다음 검을 준비할 수 없으며 이전 파괴 기록도 폐기한다.
            _sequenceOwner = null;
            _plannedSwordCount = 0;
            _launchedSwordCount = _brokenSwordCount = 0;
            _swordRecalled = false;
            RefreshThrowFeedback();
            CancelThrowPrepared(deferParenting);

            // 즉시 회수 콜백은 목록을 바꾼다. 연출 중인 검은 복귀 완료까지 목록에 남긴다.
            foreach (ThrownBossSword sword in _active.ToArray())
            {
                if (sword == null)
                    _active.Remove(sword);
                else if (animateEmbedded && !sword.IsFlying)
                    sword.BeginAnimatedRecall();
                else
                    sword.Recall(deferParenting);
            }
        }

        private void OnDisable()
        {
            _health.Died -= Clear;
            _groggy.Entered -= HandleGroggyEntered;
            if (_combatGate != null) _combatGate.AvailabilityChanged -= HandleCombatAvailability;
            CancelThrows(deferParenting: true);
        }
    }
}
