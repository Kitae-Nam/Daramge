using System.Collections;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.AnimationSystem;
using CHG._02_Scripts.CoreSystem.FeedbackSystem;
using UnityEngine;

namespace CHG._02_Scripts.Player.Combat
{
    public class JumpSkill : AbstractPlayerSkill, IChargeSkill
    {
        public bool IsHolding => ChargeLogic.IsCharging;
        [field: SerializeField] public ChargeLogic ChargeLogic { get; private set; }

        [SerializeField] private FeedbackPlayer landingFeedback;
        [SerializeField] private AnimParamSO chargeParam;
        [SerializeField] private AnimParamSO jumpParam;
        [SerializeField] private AnimParamSO landParam;
        [SerializeField] private OverlapDamageCaster damageCaster;

        [SerializeField] private float landingRecoveryTime = 0.2f;
        [SerializeField, Min(0.1f)] private float riseGravityScale = 3f; 
        [SerializeField, Min(0.1f)] private float fallGravityScale = 4f;
        
        [Header("Field Jump")]
        [SerializeField] private float minFieldHeight = 1f;
        [SerializeField] private float maxFieldHeight = 4f;
        [SerializeField] private float minFieldDistance = 2f;
        [SerializeField] private float maxFieldDistance = 6f;
        
        [Header("Combat Jump")]
        [SerializeField] private float combatHeight = 2f;
        [SerializeField] private float combatMaxDistance = 6f;
        [SerializeField] private float landingRadius = 2.5f;
        
        private ILaunchMovement _launchMovement;
        private ICombatState _combatState;
        private bool _isCombatJump;
        private float _landingPower;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _launchMovement = _player.GetModule<ILaunchMovement>();
            _combatState = _player.GetModule<ICombatState>();
            Debug.Assert(_launchMovement != null, $"LaunchMovement is null : {gameObject.name}");
            Debug.Assert(_combatState != null, $"CombatState is null : {gameObject.name}");
            Debug.Assert(damageCaster != null, $"DamageCaster is null : {gameObject.name}");
            damageCaster.InitCaster(_player);
        }

        private void OnDestroy()
        {
            if (_launchMovement != null)
                _launchMovement.OnLanded -= HandleLanded;
        }

        public override bool CanUseSkill(GameObject target = null)
            => NormalizedCooldown >= 1f && !IsUsing && !_launchMovement.IsLaunched;

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            _movement.CanManualMove = false;
            _movement.SetMovementVelocity(Vector3.zero);
            _isCombatJump = _combatState.IsCombat;

            if (_isCombatJump)
            {
                StartCombatJump();
                return;
            }
            
            _renderer.PlayClip(chargeParam.ParamHash, 0f, 0.1f);
            ChargeLogic.Begin();
        }

        private void Update()
        {
            if (!ChargeLogic.IsCharging) return;
            ChargeLogic.Tick(Time.deltaTime);
            _movement.RotateTo(GetInputDirection());
        }
        
        public void Release()
        {
            float power = ChargeLogic.Release();
            if (!ChargeLogic.IsValidCharge)
                power = 0f;

            _landingPower = power;
            float height = Mathf.Lerp(minFieldHeight, maxFieldHeight, power);
            float distance = Mathf.Lerp(minFieldDistance, maxFieldDistance, power);
            StartJump(GetInputDirection() * distance, height);
        }
        
        private void StartCombatJump()
        {
            Vector3 playerPos = _player.transform.position; 
            Vector3 toMouse = _player.PlayerInput.GetWorldMousePosition(playerPos.y) - playerPos; 
            toMouse.y = 0f;
            Vector3 displacement = Vector3.ClampMagnitude(toMouse, combatMaxDistance);
            _movement.RotateTo(displacement);
            _landingPower = 1f;
            StartJump(displacement, combatHeight);
        }

        private void StartJump(Vector3 displacement, float height)
        {
            _renderer.PlayClip(jumpParam.ParamHash, 0f, 0.05f);
            _launchMovement.OnLanded += HandleLanded;
            _launchMovement.Launch(CalculateLaunchVelocity(displacement, height), riseGravityScale, fallGravityScale);
        }

        private Vector3 CalculateLaunchVelocity(Vector3 displacement, float height)
        {
            float baseGravity = Mathf.Abs(_launchMovement.Gravity); 
            float riseGravity = baseGravity * riseGravityScale; 
            float fallGravity = baseGravity * fallGravityScale; 
            float safeHeight = Mathf.Max(height, 0.01f); 

            float verticalSpeed = Mathf.Sqrt(2f * riseGravity * safeHeight); 
            float flightTime = verticalSpeed / riseGravity + Mathf.Sqrt(2f * safeHeight / fallGravity); 

            displacement.y = 0f;
            Vector3 horizontalVelocity = displacement / flightTime;
            return new Vector3(horizontalVelocity.x, verticalSpeed, horizontalVelocity.z);
        }

        private void HandleLanded()
        {
            _launchMovement.OnLanded -= HandleLanded;
            _renderer.PlayClip(landParam.ParamHash, 0f, 0.05f);
            PlayFeedback(landingFeedback, _player.transform.position, Vector3.down, _landingPower);

            if (_isCombatJump)
            {
                damageCaster.ResetHitCaster();
                damageCaster.CastDamage(damageCaster.transform.position, SkillData, 1f, landingRadius);
            }

            StartCoroutine(LandingRecoveryCoroutine());
        }

        private IEnumerator LandingRecoveryCoroutine()
        {
            yield return new WaitForSeconds(landingRecoveryTime);
            if (IsUsing) StopSkill();
        }

        private Vector3 GetInputDirection()
            => _movement.ToWorldDirection(_player.PlayerInput.Movement);

        public override void StopSkill()
        {
            StopAllCoroutines();
            ChargeLogic.Cancel();
            _launchMovement.OnLanded -= HandleLanded;
            _launchMovement.CancelLaunch();
            _movement.CanManualMove = true;
            base.StopSkill();   
        }
    }
}