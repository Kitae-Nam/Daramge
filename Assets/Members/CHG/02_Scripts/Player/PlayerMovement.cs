using CHG._02_Scripts.Agent.StatSystem;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player
{
    public class PlayerMovement : MonoBehaviour, IModule, IControlMovement, IAfterInitModule
    {
        [SerializeField] private StatSO moveSpeedStat;
        [SerializeField] private float gravity = -9.8f;
        
        private Vector3 _velocity;
        private float _verticalVelocity;
        private Vector3 _movementDirection;
        private CharacterController _characterController;
        private ModuleOwner _owner;
        private IStatModule _statModule;
        private Vector3 _autoVelocity;
        private float _moveSpeed = 8f;

        public bool CanManualMove { get; set; } = true;
        public bool CanUseGravity { get; set; } = true;
        public Vector3 Velocity => _velocity;
        public bool IsGround => _characterController.isGrounded;

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
            Vector3 newMovement = new Vector3(movementInput.x, 0, movementInput.y).normalized;
            _movementDirection = newMovement;
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
        }

        private void CalculateMovement()
        {
            if(CanManualMove)
                _velocity = Quaternion.Euler(0, -45f, 0) * _movementDirection;
            else 
                _velocity = _autoVelocity;
            
            _velocity *= _moveSpeed * Time.fixedDeltaTime;
            if (_velocity.sqrMagnitude > 0)
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
                _verticalVelocity = -0.03f;
            }
            else
            {
                _verticalVelocity += gravity * Time.fixedDeltaTime;
            }

            _velocity.y = _verticalVelocity;
        }

        private void Move()
        {
            _characterController.Move(_velocity);
        }
    }
}