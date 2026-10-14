using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Memebers.CWH.SCRIPTS.Input
{
    [CreateAssetMenu(fileName = "Player Input", menuName = "SO/PlayerInput", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public Func<Vector2, bool> IsPointBlockBtUI;
        public event Action<Vector2> OnMovementChange;
        public event Action OnAttackKeyPressed;

        public delegate void SkillKeyPress(int keyCode, bool isPressed);

        // public event SkillKeyPress OnSkillKeyPressed;

        [SerializeField] private LayerMask whatIsGround;
        private Controls _controls;

        private Vector3 _worldMousePosition;
        private Vector2 _screenMousePosition;

        private Camera _mainCam;

        public Camera MainCam
        {
            get
            {
                if (_mainCam == null)
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
            OnMovementChange?.Invoke(movement);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (IsPointBlockBtUI != null && IsPointBlockBtUI(_screenMousePosition))
                return;

            if (context.performed)
                OnAttackKeyPressed?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnSkill(InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }
    }
}