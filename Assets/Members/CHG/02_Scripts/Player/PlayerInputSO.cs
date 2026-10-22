using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Members.CHG._02_Scripts.Player
{
     [CreateAssetMenu(fileName = "Player Input", menuName = "SO/PlayerInput", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        
        public event Action<Vector2> OnMovementChange;
        public event Action OnAttackKeyPressed;
        public event Action OnDashKeyPressed;
        public event Action OnSkillKeyPressed;
        

        public Vector2 Movement { get; private set; }

        [SerializeField] private LayerMask whatIsGround;
        private Controls _controls;

        private Vector3 _worldMousePosition;
        private Vector2 _screenMousePosition;

        private Camera _mainCam;

        public Camera MainCam
        {
            get
            {
                if(_mainCam == null)
                    _mainCam = Camera.main;
                return _mainCam;
            }
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();
            Movement = movement;

            OnMovementChange?.Invoke(movement);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnAttackKeyPressed?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            
        }
        
        public void OnSkill(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnSkillKeyPressed?.Invoke();
        }

        public void OnPointer(InputAction.CallbackContext context)
        {
            _screenMousePosition = context.ReadValue<Vector2>();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnDashKeyPressed?.Invoke();
        }

        public Vector3 GetWorldMousePosition()
        {
            if(MainCam == null)
                return _worldMousePosition;
            
            Ray camRay = MainCam.ScreenPointToRay(_screenMousePosition);
            if (Physics.Raycast(camRay, out RaycastHit hit, MainCam.farClipPlane, whatIsGround))
            {
                _worldMousePosition = hit.point;
            }
            return _worldMousePosition;
        }
    }
}