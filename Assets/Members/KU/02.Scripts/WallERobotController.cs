
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
public class WallERobotController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 13f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private Camera mainCamera;

    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction pointAction;

    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 dashDirection;

    private float dashTimer;
    private float cooldownTimer;

    private bool isDashing;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        mainCamera = Camera.main;

        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        moveAction = playerInput.actions.FindAction("Move", true);
        dashAction = playerInput.actions.FindAction("Dash", true);
        pointAction = playerInput.actions.FindAction("Pointer", true);
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        CalculateMoveDirection();
        RotateTowardsMouse();

        if (dashAction.WasPressedThisFrame())
        {
            TryDash();
        }
    }

    private void FixedUpdate()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.fixedDeltaTime;

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
                isDashing = false;
        }

        Vector3 horizontalVelocity =
            isDashing
                ? dashDirection * dashSpeed
                : moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void CalculateMoveDirection()
    {
        if (mainCamera == null)
            return;

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection =
            (right * moveInput.x +
             forward * moveInput.y).normalized;
    }

    private void RotateTowardsMouse()
    {
        if (mainCamera == null)
            return;

        Vector2 mousePosition =
            pointAction.ReadValue<Vector2>();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        Plane groundPlane = new Plane(
            Vector3.up,
            transform.position
        );

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPosition = ray.GetPoint(distance);
            Vector3 direction =
                targetPosition - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction);
            }
        }
    }

    private void TryDash()
    {
        if (isDashing || cooldownTimer > 0f)
            return;

        dashDirection = transform.forward;
        dashDirection.y = 0f;
        dashDirection.Normalize();

        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDashing)
            return;

        BreakableWall wall =
            collision.collider.GetComponentInParent<BreakableWall>();

        if (wall != null)
        {
            wall.Break();
        }
    }
}
