
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PuzzleCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PlayerInput playerInput;

    [Header("Follow Camera")]
    [SerializeField]
    private Vector3 followOffset =
        new Vector3(0f, 10f, -8f);

    [SerializeField] private float followSmoothTime = 0.15f;

    [SerializeField] private float followCameraSize = 5f;

    [SerializeField] private float followNearClip = -10f;

    [Header("Puzzle Camera")]
    [SerializeField] private float puzzleCameraSize = 7f;

    [SerializeField] private float puzzleNearClip = 0f;

    [Header("Camera Transition")]
    [SerializeField] private float transitionSmoothTime = 0.35f;
    [SerializeField] private float rotationSpeed = 6f;
    [SerializeField] private float zoomSpeed = 4f;

    [SerializeField] private float nearClipSpeed = 4f;

    private Camera cam;
    private InputAction cameraToggleAction;

    private PuzzleCameraZone currentZone;

    private Vector3 cameraVelocity;
    private Quaternion followRotation;

    private bool isPuzzleMode;

    public bool IsPuzzleMode => isPuzzleMode;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (playerTarget == null || playerInput == null)
        {
            Debug.LogError(
                "PuzzleCameraController의 참조를 설정해주세요.",
                this
            );

            enabled = false;
            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogError(
                "PlayerInput에 Input Actions가 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        cameraToggleAction =
            playerInput.actions.FindAction("CameraToggle");

        if (cameraToggleAction == null)
        {
            Debug.LogError(
                "CameraToggle 액션을 찾지 못했습니다.",
                this
            );

            enabled = false;
            return;
        }

        // 시작 시 추적 카메라 각도 저장
        followRotation = transform.rotation;

        // 시작 위치 설정
        transform.position =
            playerTarget.position + followOffset;

        // Orthographic 카메라 설정
        cam.orthographic = true;

        // 추적 모드 기본값
        cam.orthographicSize = followCameraSize;
        cam.nearClipPlane = followNearClip;
    }

    private void Update()
    {
        if (cameraToggleAction.WasPressedThisFrame())
        {
            ToggleCameraMode();
        }
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
        UpdateCameraSize();
        UpdateNearClippingPlane();
    }

    // --------------------------------
    // Camera Toggle
    // --------------------------------

    private void ToggleCameraMode()
    {
        if (currentZone == null)
            return;

        if (currentZone.CameraPoint == null)
            return;

        isPuzzleMode = !isPuzzleMode;

        cameraVelocity = Vector3.zero;

        Debug.Log(
            isPuzzleMode
                ? "퍼즐 카메라 모드"
                : "플레이어 추적 모드"
        );
    }

    // --------------------------------
    // Camera Position
    // --------------------------------

    private void UpdateCameraPosition()
    {
        if (playerTarget == null)
            return;

        Vector3 targetPosition;
        Quaternion targetRotation;
        float smoothTime;

        if (isPuzzleMode &&
            currentZone != null &&
            currentZone.CameraPoint != null)
        {
            Transform point = currentZone.CameraPoint;

            targetPosition = point.position;
            targetRotation = point.rotation;

            smoothTime = transitionSmoothTime;
        }
        else
        {
            targetPosition =
                playerTarget.position + followOffset;

            targetRotation = followRotation;

            smoothTime = followSmoothTime;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref cameraVelocity,
            Mathf.Max(smoothTime, 0.01f)
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(
                -rotationSpeed * Time.deltaTime
            )
        );
    }

    // --------------------------------
    // Orthographic Size
    // --------------------------------

    private void UpdateCameraSize()
    {
        float targetSize =
            isPuzzleMode
                ? puzzleCameraSize
                : followCameraSize;

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            1f - Mathf.Exp(
                -zoomSpeed * Time.deltaTime
            )
        );
    }

    // --------------------------------
    // Near Clipping Plane
    // --------------------------------

    private void UpdateNearClippingPlane()
    {
        float targetNearClip =
            isPuzzleMode
                ? puzzleNearClip
                : followNearClip;

        cam.nearClipPlane = Mathf.Lerp(
            cam.nearClipPlane,
            targetNearClip,
            1f - Mathf.Exp(
                -nearClipSpeed * Time.deltaTime
            )
        );
    }

    // --------------------------------
    // Puzzle Zone
    // --------------------------------

    public void EnterPuzzleZone(PuzzleCameraZone zone)
    {
        if (zone == null)
            return;

        currentZone = zone;

        Debug.Log(
            "퍼즐 구역 진입 - V키 사용 가능"
        );
    }

    public void ExitPuzzleZone(PuzzleCameraZone zone)
    {
        if (currentZone != zone)
            return;

        currentZone = null;

        isPuzzleMode = false;
        cameraVelocity = Vector3.zero;

        Debug.Log(
            "퍼즐 구역 이탈 - 추적 모드 복귀"
        );
    }

    // --------------------------------
    // Player Detection
    // --------------------------------

    public bool IsPlayerCollider(Collider other)
    {
        if (other == null || playerTarget == null)
            return false;

        if (other.transform == playerTarget ||
            other.transform.IsChildOf(playerTarget))
        {
            return true;
        }

        if (other.attachedRigidbody != null)
        {
            Transform body =
                other.attachedRigidbody.transform;

            return body == playerTarget ||
                   body.IsChildOf(playerTarget);
        }

        return false;
    }
}
