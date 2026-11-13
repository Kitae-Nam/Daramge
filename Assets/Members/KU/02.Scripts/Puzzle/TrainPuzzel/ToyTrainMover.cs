
using System;
using System.Collections;
using UnityEngine;

public class ToyTrainMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0.1f)]
    private float forwardSpeed = 3f;

    [SerializeField, Min(0.1f)]
    private float returnSpeed = 4f;

    [SerializeField, Min(0.1f)]
    private float acceleration = 6f;

    [SerializeField, Min(0.1f)]
    private float deceleration = 8f;

    [Header("Failed Route")]
    [SerializeField, Min(0f)]
    private float stopDelay = 0.8f;

    [Header("Train Rotation")]
    [SerializeField]
    private bool rotateAlongPath = true;

    [SerializeField]
    private Vector3 modelRotationOffset;

    private Vector3[] pathPositions;
    private float[] cumulativeDistances;

    private float totalDistance;
    private float currentDistance;

    private Coroutine movementCoroutine;

    private bool isMoving;
    private bool isConfigured;

    public bool IsMoving => isMoving;

    // --------------------------------
    // Route Configuration
    // --------------------------------

    public bool ConfigureRoute(Transform[] waypoints)
    {
        if (isMoving)
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

    // --------------------------------
    // Start Movement
    // --------------------------------

    public bool BeginRun(
        int destinationWaypointIndex,
        bool routeIsClear,
        Action onReturning,
        Action<bool> onFinished)
    {
        if (!isConfigured || isMoving)
            return false;

        if (destinationWaypointIndex < 0 ||
            destinationWaypointIndex >= pathPositions.Length)
            return false;

        // 항상 출발점에서 새로운 주행 시작
        currentDistance = 0f;
        ApplyTrainPose();

        float targetDistance =
            cumulativeDistances[destinationWaypointIndex];

        isMoving = true;

        movementCoroutine = StartCoroutine(
            RunRoutine(
                targetDistance,
                routeIsClear,
                onReturning,
                onFinished
            )
        );

        return true;
    }

    // --------------------------------
    // Train Movement Cycle
    // --------------------------------

    private IEnumerator RunRoutine(
        float targetDistance,
        bool routeIsClear,
        Action onReturning,
        Action<bool> onFinished)
    {
        // 1. 앞으로 이동
        yield return TravelToDistance(
            targetDistance,
            forwardSpeed
        );

        if (routeIsClear)
        {
            // 2-A. 종착역 도착
            isMoving = false;
            movementCoroutine = null;

            onFinished?.Invoke(true);
            yield break;
        }

        // 2-B. 잘못된 레일 앞에서 잠시 정지
        if (stopDelay > 0f)
            yield return new WaitForSeconds(stopDelay);

        onReturning?.Invoke();

        // 3. 지나온 경로를 따라 복귀
        yield return TravelToDistance(
            0f,
            returnSpeed
        );

        currentDistance = 0f;
        ApplyTrainPose();

        isMoving = false;
        movementCoroutine = null;

        onFinished?.Invoke(false);
    }

    // --------------------------------
    // Distance-Based Movement
    // --------------------------------

    private IEnumerator TravelToDistance(
        float targetDistance,
        float maxSpeed)
    {
        const float tolerance = 0.001f;

        float currentSpeed = 0f;

        while (Mathf.Abs(
            targetDistance - currentDistance
        ) > tolerance)
        {
            float remaining = Mathf.Abs(
                targetDistance - currentDistance
            );

            float direction =
                targetDistance > currentDistance
                    ? 1f
                    : -1f;

            // 남은 거리에 맞춰 감속
            float brakingSpeed = Mathf.Sqrt(
                2f * deceleration * remaining
            );

            float desiredSpeed = Mathf.Min(
                maxSpeed,
                brakingSpeed
            );

            // 너무 작은 속도로 멈춰 버리는 현상 방지
            desiredSpeed = Mathf.Max(
                0.08f,
                desiredSpeed
            );

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                desiredSpeed,
                acceleration * Time.deltaTime
            );

            float moveDistance = Mathf.Min(
                remaining,
                currentSpeed * Time.deltaTime
            );

            currentDistance += direction * moveDistance;

            ApplyTrainPose();

            yield return null;
        }

        currentDistance = targetDistance;
        ApplyTrainPose();
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
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        isMoving = false;
        currentDistance = 0f;

        ApplyTrainPose();
    }

    private void OnDisable()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        isMoving = false;
    }
}
