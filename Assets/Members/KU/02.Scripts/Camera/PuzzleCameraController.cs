
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PuzzleCameraController : MonoBehaviour
{
    [Serializable]
    public class PlayerCameraSetting
    {
        public Transform playerTarget;
        public PlayerInput playerInput;
        public CinemachineCamera followCamera;

        [Min(0.01f)]
        public float cameraSize = 5f;

        public float nearClip = -10f;
    }

    [Header("Player Cameras")]
    [SerializeField]
    private PlayerCameraSetting[] players =
        new PlayerCameraSetting[3];

    [Header("Starting Player")]
    [SerializeField]
    private int startingPlayerIndex = 0;

    [Header("Puzzle Camera")]
    [SerializeField]
    private CinemachineCamera puzzleCamera;

    [SerializeField, Min(0.01f)]
    private float puzzleCameraSize = 7f;

    [SerializeField]
    private float puzzleNearClip = 0f;

    [Header("Camera Priority")]
    [SerializeField]
    private int inactivePriority = 10;

    [SerializeField]
    private int activePriority = 20;

    [SerializeField]
    private int puzzlePriority = 30;

    private PuzzleCameraZone currentZone;
    private InputAction cameraToggleAction;

    private int activePlayerIndex = -1;
    private bool isPuzzleMode;

    public bool IsPuzzleMode => isPuzzleMode;
    public int ActivePlayerIndex => activePlayerIndex;

    private void Awake()
    {
        if (players == null ||
            players.Length == 0 ||
            puzzleCamera == null)
        {
            Debug.LogError(
                "카메라 참조를 설정해주세요.",
                this
            );
            enabled = false;
            return;
        }

        // 플레이어별 카메라 렌즈 설정
        foreach (PlayerCameraSetting setting in players)
        {
            if (setting == null ||
                setting.followCamera == null)
                continue;

            ApplyLens(
                setting.followCamera,
                setting.cameraSize,
                setting.nearClip
            );
        }

        // 퍼즐 카메라 렌즈 설정
        ApplyLens(
            puzzleCamera,
            puzzleCameraSize,
            puzzleNearClip
        );

        SetActivePlayer(startingPlayerIndex);
    }

    private void Update()
    {
        if (cameraToggleAction != null &&
            cameraToggleAction.WasPressedThisFrame())
        {
            ToggleCameraMode();
        }
    }

    private void LateUpdate()
    {
        UpdatePuzzleCameraTransform();
    }

    // --------------------------------
    // Player Switching
    // --------------------------------

    // 캐릭터 전환 시스템에서 호출
    public void SetActivePlayer(int index)
    {
        if (players == null ||
            index < 0 ||
            index >= players.Length)
        {
            Debug.LogWarning(
                $"잘못된 플레이어 인덱스: {index}",
                this
            );
            return;
        }

        PlayerCameraSetting setting = players[index];

        if (setting == null ||
            setting.playerTarget == null ||
            setting.playerInput == null ||
            setting.followCamera == null ||
            setting.playerInput.actions == null)
        {
            Debug.LogWarning(
                $"플레이어 {index}의 카메라 설정이 불완전합니다.",
                this
            );
            return;
        }

        InputAction action =
            setting.playerInput.actions.FindAction(
                "CameraToggle",
                false
            );

        if (action == null)
        {
            Debug.LogWarning(
                $"플레이어 {index}에 CameraToggle 액션이 없습니다.",
                this
            );
        }

        activePlayerIndex = index;
        cameraToggleAction = action;

        // 캐릭터가 변경되면 퍼즐 뷰 해제
        isPuzzleMode = false;
        currentZone = null;

        UpdateCameraPriority();

        Debug.Log(
            $"활성 플레이어 변경: {index}",
            this
        );
    }

    // --------------------------------
    // Lens Settings
    // --------------------------------

    private void ApplyLens(
        CinemachineCamera targetCamera,
        float size,
        float nearClip)
    {
        LensSettings lens = targetCamera.Lens;

        lens.ModeOverride =
            LensSettings.OverrideModes.Orthographic;

        lens.OrthographicSize = size;
        lens.NearClipPlane = nearClip;

        targetCamera.Lens = lens;
    }

    // --------------------------------
    // Puzzle Camera Toggle
    // --------------------------------

    private void ToggleCameraMode()
    {
        if (currentZone == null ||
            currentZone.CameraPoint == null)
            return;

        isPuzzleMode = !isPuzzleMode;

        if (isPuzzleMode)
            UpdatePuzzleCameraTransform();

        UpdateCameraPriority();
    }

    // --------------------------------
    // Camera Priority
    // --------------------------------

    private void UpdateCameraPriority()
    {
        // 모든 플레이어 카메라 우선순위 초기화
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null ||
                players[i].followCamera == null)
                continue;

            players[i].followCamera.Priority =
                i == activePlayerIndex
                    ? activePriority
                    : inactivePriority;
        }

        // 퍼즐 카메라 우선순위
        puzzleCamera.Priority =
            isPuzzleMode
                ? puzzlePriority
                : inactivePriority;
    }

    // --------------------------------
    // Puzzle Camera Position
    // --------------------------------

    private void UpdatePuzzleCameraTransform()
    {
        if (currentZone == null ||
            currentZone.CameraPoint == null)
            return;

        Transform point = currentZone.CameraPoint;

        puzzleCamera.transform.SetPositionAndRotation(
            point.position,
            point.rotation
        );
    }

    // --------------------------------
    // Puzzle Zone
    // --------------------------------

    public void EnterPuzzleZone(PuzzleCameraZone zone)
    {
        if (zone == null ||
            zone.CameraPoint == null)
            return;

        if (currentZone == zone)
            return;

        currentZone = zone;

        UpdatePuzzleCameraTransform();

        Debug.Log("퍼즐 구역 진입 - V키 사용 가능");
    }

    public void ExitPuzzleZone(PuzzleCameraZone zone)
    {
        if (currentZone != zone)
            return;

        currentZone = null;
        isPuzzleMode = false;

        UpdateCameraPriority();

        Debug.Log("퍼즐 구역 이탈 - 추적 카메라 복귀");
    }

    // --------------------------------
    // Player Detection
    // --------------------------------

    public bool IsPlayerCollider(Collider other)
    {
        if (other == null ||
            activePlayerIndex < 0 ||
            activePlayerIndex >= players.Length)
            return false;

        PlayerCameraSetting setting =
            players[activePlayerIndex];

        if (setting == null ||
            setting.playerTarget == null)
            return false;

        Transform target = setting.playerTarget;

        if (other.transform == target ||
            other.transform.IsChildOf(target))
            return true;

        Rigidbody body = other.attachedRigidbody;

        if (body != null)
        {
            return body.transform == target ||
                   body.transform.IsChildOf(target);
        }

        return false;
    }
}
