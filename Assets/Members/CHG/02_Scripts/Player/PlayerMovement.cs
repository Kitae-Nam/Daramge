using System;
using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerMovement : MonoBehaviour, IModule, IControlMovement, ILaunchMovement, IAfterInitModule
    {
        [SerializeField] private StatSO moveSpeedStat;
        [SerializeField] private float gravity = -9.8f;
        [SerializeField] private float groundedVerticalSpeed = -2f;
        
        private Vector3 _velocity;
        private float _verticalVelocity;
        private CharacterController _characterController;
        private ModuleOwner _owner;
        private IStatModule _statModule;
        private Vector3 _autoVelocity;
        private float _moveSpeed = 8f;
        private bool _isLaunched;
        private Vector3 _launchHorizontalVelocity;
        private Vector2 _movementInput;
        private Transform _cameraTrm;
        private float _riseGravityScale = 1f; 
        private float _fallGravityScale = 1f; 
        
        public event Action OnLanded;

        public bool CanManualMove { get; set; } = true;
        public bool CanAutoRotate { get; set; } = true;
        public bool CanUseGravity { get; set; } = true;
        public float MoveSpeed => _moveSpeed;
        public Vector3 Velocity => _velocity;
        public bool IsGround => _characterController.isGrounded;
        public bool IsLaunched => _isLaunched;
        public float Gravity => gravity;
        
        private Transform CameraTransform
        {
            get
            {
                if (_cameraTrm == null && Camera.main != null)
                    _cameraTrm = Camera.main.transform;
                return _cameraTrm;
            }
        }

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _characterController = owner.GetComponent<CharacterController>();
            _statModule = owner.GetModule<IStatModule>();
            Debug.Assert(_statModule != null, $"StatModule is null : {gameObject.name}");
            Debug.Assert(moveSpeedStat != null, $"MoveSpeedStat is null : {gameObject.name}");
        }
        
        public void AfterInit()
        {
            _moveSpeed = _statModule.SubscribeStat(moveSpeedStat, HandleMoveSpeedChange, _moveSpeed);
        }

        private void OnDestroy()
        {
            if (moveSpeedStat != null)
                _statModule?.UnSubscribeStat(moveSpeedStat, HandleMoveSpeedChange);
        }

        private void HandleMoveSpeedChange(Stat stat, float currentValue, float prevValue)
            => _moveSpeed = currentValue;

        public void SetMovementVelocity(Vector3 velocity)
        {
            _autoVelocity = velocity;
        }

        public void SetMovementDirection(Vector2 movementInput)
        {
            _movementInput = movementInput;
        }

        public Vector3 ToWorldDirection(Vector2 movementInput)
        {
            Vector3 inputDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized; 
            Transform cam = CameraTransform; 
            if (cam == null) return inputDirection; 

            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up); 
            if (forward.sqrMagnitude < 0.001f) 
                forward = Vector3.ProjectOnPlane(cam.up, Vector3.up); 

            return Quaternion.LookRotation(forward) * inputDirection; 
        }

        public void RotateTo(Vector3 direction)
        {
            if (direction.magnitude < Mathf.Epsilon) return;
            direction.y = 0;
            _owner.transform.forward = direction.normalized;
        }

        

        private void FixedUpdate()
        {
            CalculateMovement();
            ApplyGravity();
            Move();
            CheckLanding();
        }

        private void CalculateMovement()
        {
            if (_isLaunched)
            {
                _velocity = _launchHorizontalVelocity * Time.fixedDeltaTime;
                return;
            }

            _velocity = CanManualMove ? ToWorldDirection(_movementInput) : _autoVelocity;
            
            _velocity *= _moveSpeed * Time.fixedDeltaTime;
            if (CanAutoRotate && _velocity.sqrMagnitude > 0)
            {
                float rotationSpeed = 8f;
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);
                _owner.transform.rotation = Quaternion.Lerp(_owner.transform.rotation,
                                targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void ApplyGravity()
        {
            if (!CanUseGravity)
            {
                _verticalVelocity = 0f;
            }
            else if (IsGround && _verticalVelocity < 0f)
            {
                _verticalVelocity = groundedVerticalSpeed;
            }
            else
            {
                _verticalVelocity += gravity * GetGravityScale() * Time.fixedDeltaTime;
            }

            _velocity.y = _verticalVelocity * Time.fixedDeltaTime;
        }
        private float GetGravityScale() 
            => !_isLaunched ? 1f : _verticalVelocity > 0f ? _riseGravityScale : _fallGravityScale;

        private void Move()
        {
            _characterController.Move(_velocity);
        }

        public void Launch(Vector3 launchVelocity, float riseGravityScale = 1f, float fallGravityScale = 1f)
        {
            _launchHorizontalVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);
            _verticalVelocity = launchVelocity.y;
            _riseGravityScale = riseGravityScale; 
            _fallGravityScale = fallGravityScale; 
            _isLaunched = true;
        }

        public void CancelLaunch()
        {
            _isLaunched = false;
            _launchHorizontalVelocity = Vector3.zero;
            _riseGravityScale = 1f; 
            _fallGravityScale = 1f; 
        }

        private void CheckLanding()
        {
            if (!_isLaunched || _verticalVelocity > 0f || !IsGround) return;
            CancelLaunch();
            OnLanded?.Invoke();
        }
    }
}