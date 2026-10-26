using UnityEngine;
using UnityEngine.Events;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>검 자세와 시각 연출만 담당한다. 명중/체력/쿨타임을 변경하지 않는다.</summary>
    public sealed class BossCombatPresentation : MonoBehaviour
    {
        [SerializeField] private Transform _weaponPivot;
        [SerializeField] private Transform _bodyVisual;
        [SerializeField] private UnityEvent _impact = new UnityEvent();
        private Quaternion _bodyRotation;
        private Vector3 _bodyScale;
        private BossGroggyController _groggy;
        private float _pulse;
        private void Awake()
        {
            if (_bodyVisual != null) { _bodyRotation = _bodyVisual.localRotation; _bodyScale = _bodyVisual.localScale; }
            _groggy = GetComponent<BossGroggyController>();
        }
        private void Update()
        {
            if (_bodyVisual == null) return;
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 3f);
            _bodyVisual.localScale = _bodyScale * (1f + _pulse * 0.08f);
            _bodyVisual.localRotation = _bodyRotation * Quaternion.Euler(0f, 0f, _groggy != null && _groggy.IsGroggy ? 14f : 0f);
        }
        public void SetSwing(float progress, int strike)
        {
            if (_weaponPivot == null) return;
            float angle = Mathf.Lerp(-75f, 75f, Mathf.SmoothStep(0f, 1f, progress));
            _weaponPivot.localRotation = Quaternion.Euler(0f, strike % 2 == 0 ? angle : -angle, 0f);
        }
        public void SetThrust(bool active) { if (_weaponPivot != null) _weaponPivot.localRotation = Quaternion.Euler(active ? 25f : 0f, 0f, 0f); }
        public void SetWeaponVisible(bool visible) { if (_weaponPivot != null) _weaponPivot.gameObject.SetActive(visible); }
        public void ResetWeapon() { if (_weaponPivot != null) _weaponPivot.localRotation = Quaternion.identity; }
        // UnityEvent에는 준비된 소리/파티클을 연결할 수 있고, 기본 상태에서도 몸체 펄스가 표시된다.
        public void PlayImpact() { _pulse = 1f; _impact.Invoke(); }
        private void OnDisable()
        {
            ResetWeapon();
            if (_bodyVisual != null) { _bodyVisual.localRotation = _bodyRotation; _bodyVisual.localScale = _bodyScale; }
        }
    }
}
