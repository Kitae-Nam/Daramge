using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
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

        [Header("근접 검 장착 자세")]  // 집에 가고 싶어요 샤ㅑㅑㅑㅑㅑ갈
        [SerializeField] private Vector3 _heldLocalPosition;

        [SerializeField] private Vector3 _heldLocalEulerAngles;

        private SwordSwingMotion _motion;
        private float _chargeStartAngle;
        private SwordSwingTrail _swingTrail;

        public void BeginCharge(int strike)
        {
            if (_weaponPivot == null) return;
            _swingTrail?.StopAndClear();
            _chargeStartAngle = Mathf.DeltaAngle(0f, _weaponPivot.localEulerAngles.y);
            SetCharge(0f, strike);
        }

        /// <summary>경고 앞부분은 천천히 당기고 마지막 구간은 같은 자세로 버틴다.</summary>
        public void SetCharge(float progress, int strike)
        {
            if (_weaponPivot == null) return;
            float t = _motion.ChargeFraction(progress);
            _weaponPivot.localRotation = Quaternion.Euler(0f,
                Mathf.Lerp(_chargeStartAngle, _motion.ChargeAngle(strike), t), 0f);
        }

        public void BeginSlash() => _swingTrail?.Begin();
        public void EndSlash() => _swingTrail?.End();

        private Transform _heldSword;
        private SwordPoseBinding _binding;

        private Quaternion _bodyRotation;
        private Vector3 _bodyScale;
        private BossGroggyController _groggy;
        private float _pulse;

        public bool HasWeaponPivot => _weaponPivot != null;


        private void Awake()
        {
            _motion = MeleeMotionSource.Resolve(gameObject);
            if (_bodyVisual != null)
            {
                _bodyRotation = _bodyVisual.localRotation;
                _bodyScale = _bodyVisual.localScale;
            }

            _groggy = GetComponent<BossGroggyController>();
        }

        private void Update()
        {
            if (_bodyVisual == null) return;
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 3f);
            _bodyVisual.localScale = _bodyScale * (1f + _pulse * 0.08f);
            _bodyVisual.localRotation =
                _bodyRotation * Quaternion.Euler(0f, 0f, _groggy != null && _groggy.IsGroggy ? 14f : 0f);
        }

        public void SetSwing(float progress, int strike)
        {
            if (_weaponPivot == null) return;
            _weaponPivot.localRotation = Quaternion.Euler(0f, _motion.Angle(progress, strike), 0f);
        }

        /// <summary>
        /// 실제 휘두르기 피벗에 연결합니다.
        /// 복귀할때 이전 계층과 자세를 복원하도록 장착전 정보를 저장합니다.
        /// </summary>
        public bool TryAttachSword(Transform sword)
        {
            if (_weaponPivot == null || sword == null || _heldSword != null) return false;
            SwordPoseBinding binding = sword.GetComponent<SwordPoseBinding>();
            if (binding == null) binding = sword.gameObject.AddComponent<SwordPoseBinding>();
            if (!binding.TryCapture()) return false;
            _binding = binding;
            _heldSword = sword;
            _swingTrail = sword.GetComponent<BossSword>()?.SwingTrail;
            _swingTrail?.StopAndClear();

            _weaponPivot.gameObject.SetActive(true);
            ResetWeapon();

            sword.SetParent(_weaponPivot, false);
            sword.localPosition = _heldLocalPosition;
            sword.localRotation = Quaternion.Euler(_heldLocalEulerAngles);

            return true;
        }

        /// <summary>
        /// 장착한 검의 원래 부모와 자세를 복원합니다.
        /// 반복 호출 되어도 이미 분리한 검을 다시 처리하지 않습니다.
        /// </summary>
        public void DetachSword()
        {
            _swingTrail?.StopAndClear();
            if (_binding != null) _binding.Restore();

            _heldSword = null;
            _swingTrail = null;
            _binding = null;
            ResetWeapon();
        }

        public void SetThrust(bool active)
        {
            if (_weaponPivot != null) _weaponPivot.localRotation = Quaternion.Euler(active ? 25f : 0f, 0f, 0f);
        }

        public void SetWeaponVisible(bool visible)
        {
            if (_weaponPivot != null) _weaponPivot.gameObject.SetActive(visible);
        }

        public void ResetWeapon()
        {
            if (_weaponPivot != null) _weaponPivot.localRotation = Quaternion.identity;
        }

        // UnityEvent에는 준비된 소리/파티클을 연결할 수 있고, 기본 상태에서도 몸체 펄스가 표시된다.
        public void PlayImpact()
        {
            _pulse = 1f;
            _impact.Invoke();
        }

        private void OnDisable()
        {
            DetachSword();
            if (_bodyVisual != null)
            {
                _bodyVisual.localRotation = _bodyRotation;
                _bodyVisual.localScale = _bodyScale;
            }
        }
    }
}
