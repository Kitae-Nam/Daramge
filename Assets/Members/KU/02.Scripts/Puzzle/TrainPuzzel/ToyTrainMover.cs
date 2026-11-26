using CHG._02_Scripts.Player;
using UnityEngine;

public class ToyTrainMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0.1f)]
    private float forwardSpeed = 3f;

    [SerializeField, Min(0.1f)]
    private float acceleration = 6f;

    [SerializeField, Min(0.1f)]
    private float deceleration = 8f;

    [Header("Train Rotation")]
    [SerializeField]
    private bool rotateAlongPath = true;

    [SerializeField]
    private Vector3 modelRotationOffset;

    [SerializeField] private PlayerMovement interactPlayer;

    private Vector3[] pathPositions;
    private float[] cumulativeDistances;

    private float totalDistance;
    private float currentDistance;

    private float currentSpeed;
    private bool isConfigured;

    public bool IsMoving => currentSpeed > 0f;

    // --------------------------------
    // Route Configuration
    // --------------------------------

    public bool ConfigureRoute(Transform[] waypoints)
    {
        if (IsMoving)
            return false;

        if (waypoints == null || waypoints.Length < 2)
            return false;

        pathPositions = new Vector3[waypoints.Length];

        cumulativeDistances =
            new float[waypoints.Length];

        totalDistance = 0f;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                return false;

            pathPositions[i] = waypoints[i].position;

            if (i == 0)
            {
                cumulativeDistances[i] = 0f;
                continue;
            }

            float segmentDistance = Vector3.Distance(
                pathPositions[i - 1],
                pathPositions[i]
            );

            // 겹친 Waypoint 방지
            if (segmentDistance < 0.001f)
            {
                Debug.LogError(
                    $"Waypoint {i - 1}과 {i}가 겹칩니다.",
                    this
                );
                return false;
            }

            totalDistance += segmentDistance;

            cumulativeDistances[i] = totalDistance;
        }

        isConfigured = true;
        currentDistance = 0f;

        ApplyTrainPose();

        return true;
    }

    // Brake for the next blocked rail or station. Stop this frame at the checkpoint
    // so the manager can lock a correct rail before the train enters it.
    internal bool AdvanceToCheckpoint(int checkpointIndex, int stopIndex)
    {
        float checkpointDistance = cumulativeDistances[checkpointIndex];
        float stopDistance = cumulativeDistances[stopIndex];
        float remaining = Mathf.Max(0f, stopDistance - currentDistance);
        float deltaTime = Time.deltaTime;
        float brakingSpeed = Mathf.Sqrt(2f * deceleration * remaining);
        float desiredSpeed = Mathf.Min(forwardSpeed, Mathf.Max(0.08f, brakingSpeed));
        float speedChange = desiredSpeed < currentSpeed ? deceleration : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, speedChange * deltaTime);

        currentDistance = Mathf.MoveTowards(currentDistance, checkpointDistance, currentSpeed * deltaTime);
        if (currentDistance >= stopDistance)
            currentSpeed = 0f;

        ApplyTrainPose();
        return currentDistance >= checkpointDistance;
    }

    // --------------------------------
    // Position Along Route
    // --------------------------------

    private Vector3 GetPositionAtDistance(
        float distance)
    {
        distance = Mathf.Clamp(
            distance,
            0f,
            totalDistance
        );

        for (int i = 0;
             i < cumulativeDistances.Length - 1;
             i++)
        {
            float startDistance =
                cumulativeDistances[i];

            float endDistance =
                cumulativeDistances[i + 1];

            if (distance > endDistance &&
                i < cumulativeDistances.Length - 2)
                continue;

            float t = Mathf.InverseLerp(
                startDistance,
                endDistance,
                distance
            );

            return Vector3.Lerp(
                pathPositions[i],
                pathPositions[i + 1],
                t
            );
        }

        return pathPositions[pathPositions.Length - 1];
    }

    // --------------------------------
    // Apply Train Position / Rotation
    // --------------------------------

    private void ApplyTrainPose()
    {
        if (!isConfigured)
            return;

        transform.position =
            GetPositionAtDistance(currentDistance);

        if (!rotateAlongPath)
            return;

        // 앞뒤의 경로 좌표로 기차 진행 방향 계산
        float before = Mathf.Max(
            0f,
            currentDistance - 0.05f
        );

        float after = Mathf.Min(
            totalDistance,
            currentDistance + 0.05f
        );

        Vector3 direction =
            GetPositionAtDistance(after) -
            GetPositionAtDistance(before);

        // 출발점/도착점에서도 방향 계산
        if (direction.sqrMagnitude < 0.000001f)
        {
            direction =
                pathPositions[1] -
                pathPositions[0];
        }

        if (direction.sqrMagnitude > 0.000001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                ) *
                Quaternion.Euler(modelRotationOffset);
        }
    }

    // --------------------------------
    // Stop / Reset
    // --------------------------------

    public void StopAndReset()
    {
        currentSpeed = 0f;
        currentDistance = 0f;
        ApplyTrainPose();
    }

    private void OnDisable()
    {
        currentSpeed = 0f;
    }
}
