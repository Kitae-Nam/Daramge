
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using CHG._02_Scripts.Player;

public class PuzzleCameraController : MonoBehaviour
{
    [Serializable]
    public class PlayerSetting
    {
        [Header("Player")]
        public Transform playerTarget;
    }

    [Header("Character Change System")]
    [SerializeField]
    private CharacterChange characterChange;

    [Header("Player References")]
    [SerializeField]
    private PlayerSetting[] players =
        new PlayerSetting[3];

    [Header("Puzzle Camera")]
    [SerializeField]
    private CinemachineCamera puzzleCamera;

    [Header("Output Camera")]
    [SerializeField]
    private Camera outputCamera;

    [Header("Auto Camera Settings")]
    [SerializeField]
    private bool autoSwitchOnEnter = true;

    [Header("Puzzle Camera Priority")]
    [SerializeField]
    private int puzzleActivePriority = 100;

    [SerializeField]
    private int puzzleInactivePriority = -100;

    [Header("Smooth Transition")]
    [SerializeField, Min(0.01f)]
    private float positionSmoothTime = 0.35f;

    [SerializeField, Min(0.1f)]
    private float rotationSmoothSpeed = 8f;

    [SerializeField, Min(0.01f)]
    private float lensSmoothTime = 0.25f;

    private PuzzleCameraZone currentZone;

    private int activePlayerIndex = -1;

    private bool isPuzzleMode;
    private bool wasChanging;

    private Vector3 positionVelocity;
    private float lensVelocity;

    private float targetOrthographicSize;
    private float targetNearClipPlane;

    public bool IsPuzzleMode => isPuzzleMode;
    public int ActivePlayerIndex => activePlayerIndex;

    // --------------------------------
    // Initialize
    // --------------------------------

    private void Awake()
    {
        if (characterChange == null ||
            puzzleCamera == null)
        {
            Debug.LogError(
                "PuzzleCameraController 참조를 확인해주세요.",
                this
            );

            enabled = false;
            return;
        }

        if (outputCamera == null)
            outputCamera = Camera.main;

        if (outputCamera == null)
        {
            Debug.LogError(
                "Output Camera를 지정해주세요.",
                this
            );

            enabled = false;
            return;
        }

        isPuzzleMode = false;
        activePlayerIndex = -1;

        targetOrthographicSize =
            puzzleCamera.Lens.OrthographicSize;

        targetNearClipPlane =
            puzzleCamera.Lens.NearClipPlane;

        puzzleCamera.Priority =
            puzzleInactivePriority;
    }

    private void Start()
    {
        SyncCurrentPlayer();
    }

    // --------------------------------
    // Update
    // --------------------------------

    private void Update()
    {
        if (characterChange.IsChanging)
        {
            if (!wasChanging)
            {
                wasChanging = true;

                ExitPuzzleMode();
                ClearPuzzleZone();
            }

            return;
        }

        wasChanging = false;

        SyncCurrentPlayer();

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            ToggleCameraMode();
        }
    }

    private void LateUpdate()
    {
        // 퍼즐 카메라가 실제로 사용될 때만 이동
        if (!isPuzzleMode)
            return;

        UpdatePuzzleCameraTransform();
        UpdatePuzzleLens();
    }

    // --------------------------------
    // Character Detection
    // --------------------------------

    private void SyncCurrentPlayer()
    {
        if (characterChange == null ||
            characterChange.IsChanging)
            return;

        int currentIndex =
            characterChange.CurrentIndex;

        if (currentIndex < 0)
            return;

        if (currentIndex >= characterChange.Count)
        {
            Debug.LogWarning(
                $"CharacterChange 인덱스 {currentIndex}가 " +
                "캐릭터 목록 범위를 벗어났습니다.",
                this
            );
            return;
        }

        if (activePlayerIndex == currentIndex)
            return;

        OnActivePlayerChanged(currentIndex);
    }

    private void OnActivePlayerChanged(int newIndex)
    {
        ExitPuzzleMode();
        ClearPuzzleZone();

        activePlayerIndex = newIndex;
    }

    // --------------------------------
    // Puzzle Camera Start Position
    // --------------------------------

    // 현재 화면에 출력되는 카메라의 위치를
    // 퍼즐 카메라의 새로운 출발점으로 설정
    private void CaptureCurrentCameraPose()
    {
        if (outputCamera == null)
            return;

        Vector3 currentPosition =
            outputCamera.transform.position;

        Quaternion currentRotation =
            outputCamera.transform.rotation;

        puzzleCamera.transform.SetPositionAndRotation(
            currentPosition,
            currentRotation
        );

        // 이전 전환의 이동 속도 초기화
        positionVelocity = Vector3.zero;

        // 현재 화면이 Orthographic이라면
        // 줌 크기도 현재 화면에서 시작
        LensSettings lens = puzzleCamera.Lens;

        if (outputCamera.orthographic)
        {
            lens.OrthographicSize =
                outputCamera.orthographicSize;
        }

        lens.ModeOverride =
            LensSettings.OverrideModes.Orthographic;

        puzzleCamera.Lens = lens;

        lensVelocity = 0f;
    }

    // --------------------------------
    // Lens Target
    // --------------------------------

    private void SetPuzzleLensTarget(
        PuzzleCameraZone zone)
    {
        targetOrthographicSize =
            zone.OrthographicSize;

        targetNearClipPlane =
            zone.NearClipPlane;
    }

    private void UpdatePuzzleLens()
    {
        if (currentZone == null ||
            puzzleCamera == null)
            return;

        LensSettings lens = puzzleCamera.Lens;

        lens.ModeOverride =
            LensSettings.OverrideModes.Orthographic;

        lens.OrthographicSize =
            Mathf.SmoothDamp(
                lens.OrthographicSize,
                targetOrthographicSize,
                ref lensVelocity,
                lensSmoothTime
            );

        lens.NearClipPlane =
            Mathf.MoveTowards(
                lens.NearClipPlane,
                targetNearClipPlane,
                Time.deltaTime * 2f
            );

        puzzleCamera.Lens = lens;
    }

    // --------------------------------
    // Puzzle Camera Activation
    // --------------------------------

    private void ActivatePuzzleMode()
    {
        if (currentZone == null ||
            currentZone.CameraPoint == null)
            return;

        // 이미 퍼즐 카메라를 사용 중이라면
        // 현재 위치를 유지한다.
        if (isPuzzleMode)
            return;

        // 새 퍼즐뷰를 켜기 전에
        // 실제 화면의 현재 위치를 복사
        CaptureCurrentCameraPose();

        SetPuzzleLensTarget(currentZone);

        isPuzzleMode = true;

        puzzleCamera.Priority =
            puzzleActivePriority;
    }

    private void ToggleCameraMode()
    {
        if (characterChange.IsChanging)
            return;

        if (activePlayerIndex < 0)
            return;

        if (currentZone == null ||
            currentZone.CameraPoint == null)
            return;

        if (isPuzzleMode)
        {
            ExitPuzzleMode();
        }
        else
        {
            ActivatePuzzleMode();
        }
    }

    private void ExitPuzzleMode()
    {
        isPuzzleMode = false;

        if (puzzleCamera != null)
        {
            puzzleCamera.Priority =
                puzzleInactivePriority;
        }
    }

    // --------------------------------
    // Smooth Puzzle Camera Movement
    // --------------------------------

    private void UpdatePuzzleCameraTransform()
    {
        if (currentZone == null ||
            currentZone.CameraPoint == null ||
            puzzleCamera == null)
            return;

        Transform point =
            currentZone.CameraPoint;

        // 순간이동하지 않고 현재 위치에서
        // 새로운 CameraPoint로 부드럽게 이동
        Vector3 nextPosition = Vector3.SmoothDamp(
            puzzleCamera.transform.position,
            point.position,
            ref positionVelocity,
            positionSmoothTime
        );

        // 현재 회전에서 목표 회전으로 이동
        float rotationT =
            1f - Mathf.Exp(
                -rotationSmoothSpeed * Time.deltaTime
            );

        Quaternion nextRotation = Quaternion.Slerp(
            puzzleCamera.transform.rotation,
            point.rotation,
            rotationT
        );

        puzzleCamera.transform.SetPositionAndRotation(
            nextPosition,
            nextRotation
        );
    }

    // --------------------------------
    // Puzzle Zone
    // --------------------------------

    public void EnterPuzzleZone(PuzzleCameraZone zone)
    {
        if (!isActiveAndEnabled ||
            zone == null ||
            zone.CameraPoint == null)
            return;

        if (characterChange == null ||
            characterChange.IsChanging)
            return;

        if (activePlayerIndex < 0)
            return;

        if (currentZone == zone)
            return;

        // 새 목표만 변경한다.
        // 기존 퍼즐뷰 사용 중이면 카메라를
        // 순간이동시키지 않는다.
        currentZone = zone;

        SetPuzzleLensTarget(zone);

        if (autoSwitchOnEnter)
        {
            // 이미 퍼즐 모드라면 현재 위치 유지
            // 복귀 중이었다면 현재 화면에서 시작
            ActivatePuzzleMode();
        }
    }

    public void ExitPuzzleZone(PuzzleCameraZone zone)
    {
        if (currentZone != zone)
            return;

        currentZone = null;

        ExitPuzzleMode();
    }

    private void ClearPuzzleZone()
    {
        if (currentZone == null)
            return;

        currentZone.ForgetTrackedPlayer();
        currentZone = null;
    }

    // --------------------------------
    // Player Detection
    // --------------------------------

    public bool IsPlayerCollider(Collider other)
    {
        if (!isActiveAndEnabled ||
            other == null ||
            characterChange == null ||
            characterChange.IsChanging ||
            activePlayerIndex < 0 ||
            activePlayerIndex >= characterChange.Count)
        {
            return false;
        }

        Transform target = null;

        if (players != null &&
            activePlayerIndex < players.Length)
        {
            target =
                players[activePlayerIndex]?.playerTarget;
        }

        if (target == null)
        {
            var player =
                characterChange.GetPlayer(
                    activePlayerIndex
                );

            if (player == null)
                return false;

            target = player.transform;
        }

        if (other.transform == target ||
            other.transform.IsChildOf(target))
        {
            return true;
        }

        Rigidbody body =
            other.attachedRigidbody;

        if (body != null)
        {
            return body.transform == target ||
                   body.transform.IsChildOf(target);
        }

        return false;
    }

    private void OnDisable()
    {
        ExitPuzzleMode();
        ClearPuzzleZone();
    }
}
