
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
public class WallERobotController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 540f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 13f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Carry Settings")]
    [SerializeField] private float pickupRange = 1.8f;
    [SerializeField] private float carryDistance = 1.0f;
    [SerializeField] private float carryHeight = 0.8f;
    [SerializeField] private float carrySmoothTime = 0.15f;
    [SerializeField] private float dropDuration = 0.25f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayHeight = 3f;
    [SerializeField] private float groundRayDistance = 8f;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private Camera mainCamera;

    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction interactAction;

    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 dashDirection;

    private float dashTimer;
    private float cooldownTimer;
    private bool isDashing;

    private PushableCrate heldCrate;
    private Vector3 carryVelocity;
    private bool isDropping;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        mainCamera = Camera.main;

        // 물리 충돌로 플레이어가 넘어지지 않도록 설정
        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        if (playerInput.actions == null)
        {
            Debug.LogError(
                "PlayerInput에 Input Actions 에셋이 없습니다.",
                this
            );
            enabled = false;
            return;
        }

        moveAction = playerInput.actions.FindAction(
            "Move", true
        );

        dashAction = playerInput.actions.FindAction(
            "Dash", true
        );

        interactAction = playerInput.actions.FindAction(
            "Interact", true
        );
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        CalculateMoveDirection();

        if (dashAction.WasPressedThisFrame())
        {
            TryDash();
        }

        if (interactAction.WasPressedThisFrame())
        {
            HandleInteract();
        }
    }

    private void FixedUpdate()
    {
        UpdateDashTimers();

        // 이동 방향으로 부드럽게 회전
        RotateTowardsMovement();

        Vector3 horizontalVelocity;

        if (isDashing)
        {
            horizontalVelocity = dashDirection * dashSpeed;
        }
        else
        {
            horizontalVelocity = moveDirection * moveSpeed;
        }

        rb.linearVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void LateUpdate()
    {
        UpdateHeldCrate();
    }

    // --------------------------------
    // Movement
    // --------------------------------

    private void CalculateMoveDirection()
    {
        if (mainCamera == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection = (
            right * moveInput.x +
            forward * moveInput.y
        ).normalized;
    }

    // --------------------------------
    // Rotation
    // --------------------------------

    private void RotateTowardsMovement()
    {
        // 돌진 중에는 방향을 변경하지 않는다.
        if (isDashing)
            return;

        // 이동하지 않으면 현재 바라보는 방향 유지
        if (moveDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

        Quaternion smoothRotation =
            Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

        rb.MoveRotation(smoothRotation);
    }

    // --------------------------------
    // Dash
    // --------------------------------

    private void UpdateDashTimers()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.fixedDeltaTime;

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }
    }

    private void TryDash()
    {
        if (isDashing || cooldownTimer > 0f)
            return;

        // 마우스가 아닌 플레이어 정면으로 돌진
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
            collision.collider
                .GetComponentInParent<BreakableWall>();

        if (wall != null)
        {
            wall.Break();
        }
    }

    // --------------------------------
    // Interaction
    // --------------------------------

    private void HandleInteract()
    {
        if (isDropping)
            return;

        if (heldCrate != null)
        {
            TryDropCrate();
        }
        else
        {
            TryPickUpCrate();
        }
    }

    // --------------------------------
    // Pick Up
    // --------------------------------

    private void TryPickUpCrate()
    {
        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                transform.position,
                pickupRange,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        PushableCrate closestCrate = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in nearbyColliders)
        {
            PushableCrate crate =
                col.GetComponentInParent<PushableCrate>();

            if (crate == null)
                continue;

            if (crate.IsHeld || crate.IsTransitioning)
                continue;

            Vector3 toCrate =
                crate.transform.position
                - transform.position;

            toCrate.y = 0f;

            // 플레이어 앞쪽에 있는 상자만 선택
            if (Vector3.Dot(
                transform.forward,
                toCrate
            ) <= 0f)
            {
                continue;
            }

            float distance = toCrate.sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCrate = crate;
            }
        }

        if (closestCrate == null)
            return;

        heldCrate = closestCrate;
        carryVelocity = Vector3.zero;

        heldCrate.PickUp();

        Debug.Log("상자를 집었습니다.");
    }

    // --------------------------------
    // Carry
    // --------------------------------

    private Vector3 GetCarryPosition()
    {
        // 캐릭터의 정면 방향을 기준으로
        // 상자를 배 앞쪽에 위치시킨다.
        return transform.position
            + transform.forward * carryDistance
            + Vector3.up * carryHeight;
    }

    private void UpdateHeldCrate()
    {
        if (heldCrate == null || isDropping)
            return;

        Vector3 targetPosition = GetCarryPosition();

        // 상자를 처음 들어 올릴 때만 부드럽게 보간
        if (heldCrate.IsTransitioning)
        {
            Vector3 smoothPosition =
                Vector3.SmoothDamp(
                    heldCrate.transform.position,
                    targetPosition,
                    ref carryVelocity,
                    carrySmoothTime
                );

            heldCrate.SetCarryPosition(smoothPosition);

            if (Vector3.Distance(
                heldCrate.transform.position,
                targetPosition
            ) < 0.08f)
            {
                heldCrate.SetCarryPosition(targetPosition);
                heldCrate.FinishPickUp();

                carryVelocity = Vector3.zero;
            }
        }
        else
        {
            // 운반 중에는 지연 없이 정면 위치 유지
            heldCrate.SetCarryPosition(targetPosition);
        }
    }

    // --------------------------------
    // Drop
    // --------------------------------

    private void TryDropCrate()
    {
        if (heldCrate == null)
            return;

        if (heldCrate.IsTransitioning)
            return;

        Vector3 dropXZ =
            transform.position
            + transform.forward * carryDistance;

        Vector3 rayOrigin =
            dropXZ + Vector3.up * groundRayHeight;

        if (!Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            groundRayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        ))
        {
            Debug.Log("내려놓을 바닥이 없습니다.");
            return;
        }

        Vector3 halfExtents = heldCrate.HalfExtents;

        Vector3 dropPosition = new Vector3(
            dropXZ.x,
            hit.point.y + halfExtents.y + 0.02f,
            dropXZ.z
        );

        Physics.SyncTransforms();

        Collider[] overlaps = Physics.OverlapBox(
            dropPosition,
            halfExtents * 0.9f,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider col in overlaps)
        {
            if ((groundLayer.value &
                (1 << col.gameObject.layer)) != 0)
            {
                continue;
            }

            if (col.transform == transform ||
                col.transform.IsChildOf(transform))
            {
                continue;
            }

            if (col.GetComponentInParent<PushableCrate>()
                == heldCrate)
            {
                continue;
            }

            Debug.Log(
                "해당 위치에는 상자를 놓을 수 없습니다."
            );
            return;
        }

        StartCoroutine(
            DropCrateRoutine(dropPosition)
        );
    }

    private IEnumerator DropCrateRoutine(
        Vector3 targetPosition
    )
    {
        isDropping = true;

        PushableCrate crate = heldCrate;

        crate.BeginDrop();

        Vector3 startPosition =
            crate.transform.position;

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(
                    dropDuration,
                    0.01f
                )
            );

            // SmoothStep 형태의 부드러운 이동
            t = t * t * (3f - 2f * t);

            Vector3 nextPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            crate.SetCarryPosition(nextPosition);

            yield return null;
        }

        crate.FinishDrop(targetPosition);

        heldCrate = null;
        carryVelocity = Vector3.zero;
        isDropping = false;

        Debug.Log("상자를 내려놓았습니다.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            pickupRange
        );
    }
}
