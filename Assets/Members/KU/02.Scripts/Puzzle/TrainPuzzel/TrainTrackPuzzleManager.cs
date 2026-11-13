
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrainTrackPuzzleManager : MonoBehaviour
{
    public enum TrainPuzzleState
    {
        Ready,
        Running,
        Returning,
        Completed
    }

    [Serializable]
    public class RailSetting
    {
        [Header("Rotating Rail")]
        public DashRotationPuzzle rail;

        [Header("Correct Rotation Step")]
        [Min(0)]
        public int requiredStep;

        [Header("Stop Before This Rail")]
        [Min(0)]
        public int stopWaypointIndex;
    }

    [Header("Train Reference")]
    [SerializeField]
    private ToyTrainMover trainMover;

    [Header("Train Route")]
    [SerializeField]
    private Transform[] waypoints;

    [Header("Rail Puzzle Settings")]
    [SerializeField]
    private List<RailSetting> railSettings = new();

    [Header("Events")]
    [SerializeField]
    private UnityEvent onTrainDeparted;

    [SerializeField]
    private UnityEvent onTrainReturning;

    [SerializeField]
    private UnityEvent onTrainReturned;

    [SerializeField]
    private UnityEvent onPuzzleSolved;

    private TrainPuzzleState currentState =
        TrainPuzzleState.Ready;

    private bool isInitialized;

    public TrainPuzzleState CurrentState => currentState;

    public bool IsSolved =>
        currentState == TrainPuzzleState.Completed;

    // --------------------------------
    // Initialize
    // --------------------------------

    private void Awake()
    {
        if (trainMover == null ||
            waypoints == null ||
            waypoints.Length < 2)
        {
            Debug.LogError(
                "기차 또는 Waypoints 설정을 확인해주세요.",
                this
            );
            return;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                Debug.LogError(
                    $"Waypoint {i}가 비어 있습니다.",
                    this
                );
                return;
            }
        }

        // 모든 회전식 레일 정보 검증
        foreach (RailSetting setting in railSettings)
        {
            if (setting == null || setting.rail == null)
            {
                Debug.LogError(
                    "Rail Settings에 비어 있는 레일이 있습니다.",
                    this
                );
                return;
            }

            if (setting.stopWaypointIndex < 0 ||
                setting.stopWaypointIndex >= waypoints.Length - 1)
            {
                Debug.LogError(
                    $"{setting.rail.name}의 정지 Waypoint " +
                    "인덱스가 잘못되었습니다.",
                    this
                );
                return;
            }
        }

        if (!trainMover.ConfigureRoute(waypoints))
            return;

        currentState = TrainPuzzleState.Ready;
        isInitialized = true;
    }

    // --------------------------------
    // Lever Interaction
    // --------------------------------

    // 레버가 호출하는 메서드
    public bool RequestTrainDeparture()
    {
        if (!isInitialized)
            return false;

        if (currentState != TrainPuzzleState.Ready)
            return false;

        if (trainMover.IsMoving)
            return false;

        // 레일이 회전 중이면 출발 불가
        foreach (RailSetting setting in railSettings)
        {
            if (setting.rail.IsRotating)
                return false;
        }

        // 기차가 이동할 최종 Waypoint
        int destinationIndex =
            waypoints.Length - 1;

        bool routeIsClear = true;

        // 모든 레일의 회전 상태 검사
        foreach (RailSetting setting in railSettings)
        {
            bool isCorrect =
                setting.rail.CurrentStep ==
                setting.requiredStep;

            if (isCorrect)
                continue;

            routeIsClear = false;

            // 여러 레일이 틀렸다면
            // 가장 먼저 만나는 레일 앞에서 정지
            destinationIndex = Mathf.Min(
                destinationIndex,
                setting.stopWaypointIndex
            );
        }

        currentState = TrainPuzzleState.Running;

        bool started = trainMover.BeginRun(
            destinationIndex,
            routeIsClear,
            HandleTrainReturning,
            HandleTrainFinished
        );

        if (!started)
        {
            currentState = TrainPuzzleState.Ready;
            return false;
        }

        Debug.Log(
            routeIsClear
                ? "모든 레일이 연결되어 기차가 출발합니다."
                : $"레일 연결 실패. Waypoint {destinationIndex}에서 정지 예정.",
            this
        );

        onTrainDeparted?.Invoke();

        return true;
    }

    // UnityEvent에서 직접 사용할 수 있는 버전
    public void StartTrain()
    {
        RequestTrainDeparture();
    }

    // --------------------------------
    // Train Callbacks
    // --------------------------------

    private void HandleTrainReturning()
    {
        currentState = TrainPuzzleState.Returning;

        Debug.Log(
            "기차가 막힌 선로 앞에서 되돌아갑니다.",
            this
        );

        onTrainReturning?.Invoke();
    }

    private void HandleTrainFinished(bool reachedGoal)
    {
        if (reachedGoal)
        {
            currentState = TrainPuzzleState.Completed;

            Debug.Log(
                "장난감 기차 퍼즐 클리어!",
                this
            );

            onPuzzleSolved?.Invoke();
        }
        else
        {
            currentState = TrainPuzzleState.Ready;

            Debug.Log(
                "기차가 출발 지점으로 복귀했습니다.",
                this
            );

            onTrainReturned?.Invoke();
        }
    }

    // --------------------------------
    // Reset
    // --------------------------------

    // 기차와 퍼즐 진행 상태만 초기화
    // 개별 레일의 회전 상태는 유지
    public void ResetPuzzle()
    {
        if (!isInitialized)
            return;

        trainMover.StopAndReset();

        currentState = TrainPuzzleState.Ready;
    }

    // --------------------------------
    // Editor Test
    // --------------------------------

    [ContextMenu("Test Start Train")]
    private void TestStartTrain()
    {
        if (Application.isPlaying)
            StartTrain();
    }

    [ContextMenu("Test Reset Train")]
    private void TestResetTrain()
    {
        if (Application.isPlaying)
            ResetPuzzle();
    }
}
