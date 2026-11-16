
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
        WaitingForRail,
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
    private UnityEvent onTrainResumed;

    [SerializeField]
    private UnityEvent onPuzzleSolved;

    [SerializeField]
    private UnityEvent onTrainStopped;

    private TrainPuzzleState currentState =
        TrainPuzzleState.Ready;

    private bool isInitialized;
    private int nextRailIndex;

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

        railSettings.Sort((left, right) => left.stopWaypointIndex.CompareTo(right.stopWaypointIndex));

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
        if (!isInitialized || !isActiveAndEnabled || trainMover == null || !trainMover.isActiveAndEnabled ||
            currentState != TrainPuzzleState.Ready)
        {
            return false;
        }

        currentState = TrainPuzzleState.Running;
        onTrainDeparted?.Invoke();
        return true;
    }

    public void StartTrain()
    {
        RequestTrainDeparture();
    }

    private void Update()
    {
        if (currentState != TrainPuzzleState.Running && currentState != TrainPuzzleState.WaitingForRail)
            return;

        if (trainMover == null || !trainMover.isActiveAndEnabled)
        {
            ResetPuzzle();
            return;
        }

        if (currentState == TrainPuzzleState.WaitingForRail)
        {
            if (!TryLockRailsAtCheckpoint(railSettings[nextRailIndex].stopWaypointIndex))
                return;

            currentState = TrainPuzzleState.Running;
            onTrainResumed?.Invoke();
            if (currentState != TrainPuzzleState.Running)
                return;
        }

        int stationIndex = waypoints.Length - 1;
        int checkpointIndex = nextRailIndex < railSettings.Count
            ? railSettings[nextRailIndex].stopWaypointIndex
            : stationIndex;
        int stopIndex = stationIndex;
        for (int i = nextRailIndex; i < railSettings.Count; i++)
        {
            if (!CanCross(railSettings[i]))
            {
                stopIndex = railSettings[i].stopWaypointIndex;
                break;
            }
        }

        if (!trainMover.AdvanceToCheckpoint(checkpointIndex, stopIndex))
            return;

        if (checkpointIndex == stationIndex)
        {
            currentState = TrainPuzzleState.Completed;
            onTrainStopped?.Invoke();
            if (currentState == TrainPuzzleState.Completed)
                onPuzzleSolved?.Invoke();
        }
        else if (!TryLockRailsAtCheckpoint(checkpointIndex))
        {
            currentState = TrainPuzzleState.WaitingForRail;
            onTrainStopped?.Invoke();
        }
    }

    private static bool CanCross(RailSetting setting)
    {
        return setting.rail != null && !setting.rail.IsRotating &&
            setting.rail.CurrentStep == setting.requiredStep;
    }

    private bool TryLockRailsAtCheckpoint(int checkpointIndex)
    {
        while (nextRailIndex < railSettings.Count && railSettings[nextRailIndex].stopWaypointIndex == checkpointIndex)
        {
            RailSetting setting = railSettings[nextRailIndex];
            if (!CanCross(setting))
                return false;

            setting.rail.SetTrainRouteLocked(true);
            nextRailIndex++;
        }

        return true;
    }

    // Reset the train and release its locks while keeping the rail orientations.
    public void ResetPuzzle()
    {
        if (!isInitialized)
            return;

        if (trainMover != null)
            trainMover.StopAndReset();

        for (int i = 0; i < nextRailIndex; i++)
        {
            DashRotationPuzzle rail = railSettings[i].rail;
            if (rail != null)
                rail.SetTrainRouteLocked(false);
        }

        nextRailIndex = 0;
        currentState = TrainPuzzleState.Ready;
        onTrainStopped?.Invoke();
    }

    private void OnDisable()
    {
        if (currentState == TrainPuzzleState.Running || currentState == TrainPuzzleState.WaitingForRail)
            ResetPuzzle();
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
