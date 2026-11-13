using System;
using CHG._02_Scripts.Player.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CHG._02_Scripts.Player
{
     [CreateAssetMenu(fileName = "Player Input", menuName = "SO/PlayerInput", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        
        public event Action<Vector2> OnMovementChange;
        public event Action<PlayerSkillSlot> OnSkillKeyPressed;
        public event Action<PlayerSkillSlot> OnSkillKeyReleased;
        public event Action OnInteractKeyPressed;
        public event Action OnLockOnKeyPressed;
        public event Action OnLockOnKeyReleased;
        

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
        
        public void EnableInput() => _controls.Player.Enable();
        public void DisableInput() => _controls.Player.Disable();

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();
            Movement = movement;

            OnMovementChange?.Invoke(movement);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnSkillKeyPressed?.Invoke(PlayerSkillSlot.Attack);
            
            if (context.canceled)   
                OnSkillKeyReleased?.Invoke(PlayerSkillSlot.Attack);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnInteractKeyPressed?.Invoke();
        }
        
        public void OnFirstSkill(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnSkillKeyPressed?.Invoke(PlayerSkillSlot.FirstSkill);
            
            if (context.canceled)
                OnSkillKeyReleased?.Invoke(PlayerSkillSlot.FirstSkill);
        }
        
        public void OnScendSkill(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnSkillKeyPressed?.Invoke(PlayerSkillSlot.SecondSkill);
            
            if (context.canceled)
                OnSkillKeyReleased?.Invoke(PlayerSkillSlot.SecondSkill);
            
        }

        public void OnPointer(InputAction.CallbackContext context)
        {
            _screenMousePosition = context.ReadValue<Vector2>();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnSkillKeyPressed?.Invoke(PlayerSkillSlot.Dash);
        }
        
        public void OnCameraToggle(InputAction.CallbackContext context)
        {

        }

        public void OnLockOn(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnLockOnKeyPressed?.Invoke();
            
            if (context.canceled)
                OnLockOnKeyReleased?.Invoke();
        }

        public Vector3 GetWorldMousePosition(float fallbackHeight = 0f)
        {
            if(MainCam == null)
                return _worldMousePosition;
            
            Ray camRay = MainCam.ScreenPointToRay(_screenMousePosition);
            if (Physics.Raycast(camRay, out RaycastHit hit, MainCam.farClipPlane, whatIsGround))
            {
                _worldMousePosition = hit.point;
            }
            else if (RaycastPlane(camRay, fallbackHeight, out Vector3 planePoint))
            {
                _worldMousePosition = planePoint;
            }
            return _worldMousePosition;
        }

        public Vector3 GetMousePositionOnPlane(float height)
        {
            if (MainCam != null && RaycastPlane(MainCam.ScreenPointToRay(_screenMousePosition), height,
                    out Vector3 planePoint))
                _worldMousePosition = planePoint;
            return _worldMousePosition;
        }

        private bool RaycastPlane(Ray ray, float height, out Vector3 point)
        {
            bool isHit = new Plane(Vector3.up, new Vector3(0f, height, 0f)).Raycast(ray, out float enter);
            point = isHit ? ray.GetPoint(enter) : Vector3.zero;
            return isHit;
        }
    }
}