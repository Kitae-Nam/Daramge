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
        private Vector3 _movementDirection;
        private CharacterController _characterController;
        private ModuleOwner _owner;
        private IStatModule _statModule;
        private Vector3 _autoVelocity;
        private float _moveSpeed = 8f;
        private bool _isLaunched;
        private Vector3 _launchHorizontalVelocity;
        public event Action OnLanded;

        public bool CanManualMove { get; set; } = true;
        public bool CanAutoRotate { get; set; } = true;
        public bool CanUseGravity { get; set; } = true;
        public float MoveSpeed => _moveSpeed;
        public Vector3 Velocity => _velocity;
        public bool IsGround => _characterController.isGrounded;
        public bool IsLaunched => _isLaunched;
        public float Gravity => gravity;

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
            _moveSpeed = _statModule.SubscribeStat(moveSpeedStat.AssetIndex, HandleMoveSpeedChange, _moveSpeed);
        }

        private void OnDestroy()
        {
            if (moveSpeedStat != null)
                _statModule?.UnSubscribeStat(moveSpeedStat.AssetIndex, HandleMoveSpeedChange);
        }

        private void HandleMoveSpeedChange(StatSO stat, float currentValue, float prevValue)
            => _moveSpeed = currentValue;

        public void SetMovementVelocity(Vector3 velocity)
        {
            _autoVelocity = velocity;
        }

        public void SetMovementDirection(Vector2 movementInput)
        {
            _movementDirection = ToWorldDirection(movementInput);
        }

        public Vector3 ToWorldDirection(Vector2 movementInput)
            => Quaternion.Euler(0, -45f, 0) * new Vector3(movementInput.x, 0, movementInput.y).normalized;

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

            _velocity = CanManualMove ? _movementDirection : _autoVelocity;
            
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
                _verticalVelocity += gravity * Time.fixedDeltaTime;
            }

            _velocity.y = _verticalVelocity * Time.fixedDeltaTime;
        }

        private void Move()
        {
            _characterController.Move(_velocity);
        }

        public void Launch(Vector3 launchVelocity)
        {
            _launchHorizontalVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);
            _verticalVelocity = launchVelocity.y;
            _isLaunched = true;
        }

        public void CancelLaunch()
        {
            _isLaunched = false;
            _launchHorizontalVelocity = Vector3.zero;
        }

        private void CheckLanding()
        {
            if (!_isLaunched || _verticalVelocity > 0f || !IsGround) return;
            CancelLaunch();
            OnLanded?.Invoke();
        }
    }
}