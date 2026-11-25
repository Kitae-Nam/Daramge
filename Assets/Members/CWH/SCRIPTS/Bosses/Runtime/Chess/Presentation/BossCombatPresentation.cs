using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Unity.U2D.Physics;
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
        [SerializeField] private UnityEvent _roarImpact = new UnityEvent();

        [Header("근접 검 장착 자세")] // 집에 가고 싶어요 샤ㅑㅑㅑㅑㅑ갈 Tlqkf Tprtm
        [SerializeField]
        private Vector3 _heldLocalPosition;

        [SerializeField] private Vector3 _heldLocalEulerAngles;

        [Header("근접 겁 크기 연출")] [SerializeField, Min(1f)]
        private float _meleeSwordScaleMultiplier = 1.5f;
        
        [Header("포효 차징 연출")]
        [SerializeField, Min(1f)] private float _roarScaleMultiplier = 1.5f;
        
        [Header("검 연출")]
        [SerializeField] private UnityEvent _swordEmbedded = new UnityEvent();
        [SerializeField] private UnityEvent _swordBroken = new UnityEvent();
        
        
        private SwordSwingMotion _motion;
        private SwordSwingTrail _swingTrail;
        private Vector3 _heldBaseScale;
        private Vector3 _chargeStartScale;
        private float _chargeStartAngle;
        private float _roarChargeProgress;
        private TextMesh _swordProgress;
        private bool _dashRecovery;

        public void PlaySwordEmbedded() => _swordEmbedded.Invoke();
        public void PlaySwordBroken() => _swordBroken.Invoke();

        public void ShowSwordBreakProgress(int broken, int total, bool recalled)
        {
            if (total == 0)
            {
                if (_swordProgress != null) _swordProgress.gameObject.SetActive(false);
                return;
            }

            if (_swordProgress == null)
            {
                // 전투 공간 안에 진행도를 두면 플레이어가 박힌 검에서 보스로 바로 시선을 옮길 수 있다.
                var marker = new GameObject("Thrown Sword Progress");
                marker.transform.SetParent(transform, false);
                _swordProgress = marker.AddComponent<TextMesh>();
                _swordProgress.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _swordProgress.GetComponent<MeshRenderer>().sharedMaterial = _swordProgress.font.material;
                _swordProgress.anchor = TextAnchor.MiddleCenter;
                _swordProgress.alignment = TextAlignment.Center;
                _swordProgress.fontSize = 64;
                _swordProgress.characterSize = 0.075f;
            }

            _swordProgress.text = recalled ? $"×  {broken}/{total}" : $"{broken}/{total}";
            _swordProgress.color = recalled ? new Color(0.8f, 0.8f, 0.8f) : new Color(1f, 0.81f, 0.34f);
            _swordProgress.gameObject.SetActive(true);
        }

        public void PulseSwordBreak() => _pulse = 1f;

        private void LateUpdate()
        {
            if (_swordProgress == null || !_swordProgress.gameObject.activeSelf) return;
            Camera view = Camera.main;
            if (view == null) return;
            Vector3 position = transform.position + Vector3.up * 3f;
            _swordProgress.transform.SetPositionAndRotation(position, view.transform.rotation);
        }
        
        public void BeginCharge(int strike)
        {
            if (_weaponPivot == null) return;
            _swingTrail?.StopAndClear();
            _chargeStartAngle = Mathf.DeltaAngle(0f, _weaponPivot.localEulerAngles.y);
            if (_heldSword != null)
                _chargeStartScale = _heldSword.localScale;
            SetCharge(0f, strike);
        }

        /// <summary>경고 앞부분은 천천히 당기고 마지막 구간은 같은 자세로 버틴다.</summary>
        public void SetCharge(float progress, int strike)
        {
            if (_weaponPivot == null) return;
            float t = _motion.ChargeFraction(progress);
            _weaponPivot.localRotation = Quaternion.Euler(0f,
                Mathf.Lerp(_chargeStartAngle, _motion.ChargeAngle(strike), t), 0f);
            if (_heldSword != null)
            {
                Vector3 targetScale = _heldBaseScale * _meleeSwordScaleMultiplier;
                _heldSword.localScale = Vector3.Lerp(_chargeStartScale, targetScale, t);
                
            }
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

            float roarScale = Mathf.Lerp(1f, _roarScaleMultiplier, _roarChargeProgress);
            float impactScale = 1f + _pulse * 0.08f;
            _bodyVisual.localScale = _bodyScale * (roarScale * impactScale);
            // _bodyVisual.localScale = _bodyScale * (1f + _pulse * 0.08f);
            // _bodyVisual.localRotation =
            //     _bodyRotation * Quaternion.Euler(0f, 0f, _groggy != null && _groggy.IsGroggy ? 14f : 0f);
            
            
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
            _heldBaseScale = sword.localScale;

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

        public void SetDashRecovery(bool active)
        {
            if (_dashRecovery == active) return;
            _dashRecovery = active;
            // 본체와 무기만 기울여 반격 타이밍을 알린다. 이동과 충돌 판정은 그대로 유지한다.
            if (_bodyVisual != null)
                _bodyVisual.localRotation = _bodyRotation;
            if (_weaponPivot != null)
                _weaponPivot.localRotation = Quaternion.Euler(active ? -22f : 0f, 0f, 0f);
            if (active) _pulse = 0.65f;
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

        public void SetRoarCharge(float progress)
        {
            _roarChargeProgress = Mathf.Clamp01(progress);
        }

        public void PlayRoarImpact()
        {
            PlayImpact();
            _roarImpact.Invoke();
        }

        private void OnDisable()
        {
            ShowSwordBreakProgress(0, 0, false);
            SetDashRecovery(false);
            DetachSword();
            SetRoarCharge(0f);
            if (_bodyVisual != null)
            {
                _bodyVisual.localRotation = _bodyRotation;
                _bodyVisual.localScale = _bodyScale;
            }
        }
    }
}
