using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>필드에 있는 보스 검과 파괴 보상을 관리한다. 패턴 후딜이 끝나도 박힌 검은 유지한다.</summary>
    [RequireComponent(typeof(BossHealth), typeof(BossGroggyController))]
    public sealed class BossSwordController : MonoBehaviour
    {
        [SerializeField] private ThrownBossSword _swordPrefab;
        [SerializeField, Min(0f)] private float _breakBonusDamage = 100f;
        private BossHealth _health;
        private BossGroggyController _groggy;
        private BossCombatPresentation _presentation;
        public ThrownBossSword ActiveSword { get; private set; }
        public bool HasSword => ActiveSword != null;
        public bool CanThrow => isActiveAndEnabled && !HasSword && _swordPrefab != null && !_health.IsDead;
        private void Awake()
        {
            _health = GetComponent<BossHealth>(); _groggy = GetComponent<BossGroggyController>();
            _presentation = GetComponent<BossCombatPresentation>();
        }
        private void OnEnable() { _health.Died += Clear; _groggy.Entered += Clear; }
        public ThrownBossSword Throw(Transform target, Vector3 destination, float radius)
        {
            if (!CanThrow) return null;
            ActiveSword = Instantiate(_swordPrefab);
            ActiveSword.Finished += HandleFinished;
            ActiveSword.Launch(transform, target, transform.position + Vector3.up * 4f, destination, radius);
            if (_presentation != null) _presentation.SetWeaponVisible(false);
            return ActiveSword;
        }
        private void HandleFinished(ThrownBossSword sword, bool broken)
        {
            if (ActiveSword != sword) return;
            sword.Finished -= HandleFinished;
            ActiveSword = null;
            if (_presentation != null) _presentation.SetWeaponVisible(true);
            if (!broken || _health.IsDead || !isActiveAndEnabled) return;
            // 보너스 피해를 먼저 주므로 이 보상 자체에 새 그로기의 1.5배가 소급 적용되지는 않는다.
            _health.ReceiveDamage(_breakBonusDamage);
            if (!_health.IsDead) _groggy.TryEnterGroggy();
        }
        public void Clear() { if (ActiveSword != null) ActiveSword.Recall(); }
        private void OnDisable() { _health.Died -= Clear; _groggy.Entered -= Clear; Clear(); }
    }
}
