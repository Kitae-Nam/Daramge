
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

    [SerializeField, Min(0.01f)]
    private float puzzleCameraSize = 7f;

    [SerializeField]
    private float puzzleNearClip = 0f;

    [Header("Puzzle Camera Priority")]
    [SerializeField]
    private int puzzleActivePriority = 100;

    [SerializeField]
    private int puzzleInactivePriority = -100;

    private PuzzleCameraZone currentZone;

    private int activePlayerIndex = -1;

    private bool isPuzzleMode;
    private bool wasChanging;

    public bool IsPuzzleMode => isPuzzleMode;
    public int ActivePlayerIndex => activePlayerIndex;

    // --------------------------------
    // Initialize
    // --------------------------------

    private void Awake()
    {
        if (characterChange == null ||
            puzzleCamera == null ||
            players == null ||
            players.Length == 0)
        {
            Debug.LogError(
                "PuzzleCameraController 참조를 확인해주세요.",
                this
            );

            enabled = false;
            return;
        }

        ApplyPuzzleLens();

        isPuzzleMode = false;
        activePlayerIndex = -1;

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
        // 팀원 시스템의 캐릭터 전환 상태 확인
        if (characterChange.IsChanging)
        {
            if (!wasChanging)
            {
                wasChanging = true;

                // 전환 시작 시 퍼즐 뷰 종료
                ExitPuzzleMode();
                ClearPuzzleZone();
            }

            // 전환 중에는 V키 사용하지 않음
            return;
        }

        wasChanging = false;

        // 현재 캐릭터가 변경되었는지 자동 확인
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
        UpdatePuzzleCameraTransform();
    }

    // --------------------------------
    // Character Detection
    // --------------------------------

    private void SyncCurrentPlayer()
    {
        if (characterChange == null ||
            characterChange.IsChanging)
            return;

        int currentIndex = characterChange.CurrentIndex;

        // 아직 팀원 시스템이 초기화되지 않음
        if (currentIndex < 0)
            return;

        if (currentIndex >= players.Length)
        {
            Debug.LogWarning(
                $"CharacterChange 인덱스 {currentIndex}가 " +
                "Player References 배열 범위를 벗어났습니다.",
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
        // 이전 캐릭터의 퍼즐 상태 초기화
        ExitPuzzleMode();
        ClearPuzzleZone();

        activePlayerIndex = newIndex;

    }

    // --------------------------------
    // Lens Settings
    // --------------------------------

    private void ApplyPuzzleLens()
    {
        LensSettings lens = puzzleCamera.Lens;

        lens.ModeOverride =
            LensSettings.OverrideModes.Orthographic;

        lens.OrthographicSize =
            puzzleCameraSize;

        lens.NearClipPlane =
            puzzleNearClip;

        puzzleCamera.Lens = lens;
    }

    // --------------------------------
    // Puzzle Mode
    // --------------------------------

    private void ToggleCameraMode()
    {
        if (characterChange.IsChanging)
            return;

        if (activePlayerIndex < 0)
            return;

        if (currentZone == null ||
            currentZone.CameraPoint == null)
            return;

        isPuzzleMode = !isPuzzleMode;

        if (isPuzzleMode)
        {
            UpdatePuzzleCameraTransform();

            puzzleCamera.Priority =
                puzzleActivePriority;


        }
        else
        {
            puzzleCamera.Priority =
                puzzleInactivePriority;

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
    // Puzzle Camera Transform
    // --------------------------------

    private void UpdatePuzzleCameraTransform()
    {
        if (currentZone == null ||
            currentZone.CameraPoint == null ||
            puzzleCamera == null)
        {
            return;
        }

        Transform point =
            currentZone.CameraPoint;

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

        if (characterChange == null ||
            characterChange.IsChanging)
            return;

        if (activePlayerIndex < 0)
            return;

        if (currentZone == zone)
            return;

        currentZone = zone;

        UpdatePuzzleCameraTransform();

        
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
        if (other == null ||
            characterChange == null ||
            characterChange.IsChanging ||
            activePlayerIndex < 0 ||
            activePlayerIndex >= players.Length)
        {
            return false;
        }

        PlayerSetting setting =
            players[activePlayerIndex];

        if (setting == null ||
            setting.playerTarget == null)
        {
            return false;
        }

        Transform target = setting.playerTarget;

        // CharacterController 포함
        if (other.transform == target ||
            other.transform.IsChildOf(target))
        {
            return true;
        }

        // Rigidbody 플레이어도 지원
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
