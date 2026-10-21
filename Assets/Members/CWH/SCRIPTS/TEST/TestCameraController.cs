using UnityEngine;
using UnityEngine.InputSystem;

namespace Memebers.CWH.SCRIPTS.TEST
{
    [RequireComponent(typeof(Rigidbody))]
    public class TestCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Transform cameraTransform;

        private Rigidbody _rigidbody;
        private Vector2 _movementInput;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            _movementInput = Vector2.zero;

            if (Keyboard.current == null) return;

            if (Keyboard.current.wKey.isPressed) _movementInput.y += 1;
            if (Keyboard.current.sKey.isPressed) _movementInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) _movementInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) _movementInput.x += 1;
        }

        private void FixedUpdate()
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection =
                (cameraForward * _movementInput.y +
                 cameraRight * _movementInput.x).normalized;

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;

            _rigidbody.linearVelocity = velocity;
        }
    }
}