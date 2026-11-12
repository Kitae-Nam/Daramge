
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DashRotationPuzzle : MonoBehaviour
{
    [Header("Rotation Target")]
    [SerializeField]
    private Transform rotatingObject;

    [Header("Dash Player Reference")]
    [SerializeField]
    private Transform dashPlayer;

    [Header("Rotation Settings")]
    [SerializeField, Min(2)]
    private int totalSteps = 4;

    [SerializeField]
    private float rotationAngle = 90f;

    [SerializeField, Min(0.01f)]
    private float rotationDuration = 0.4f;

    [SerializeField]
    private bool smoothRotation = true;

    [Header("Hit Direction")]
    [SerializeField]
    private bool reverseDirection = false;

    [SerializeField, Min(0f)]
    private float centerDeadZone = 0.1f;

    [Header("Rotation Obstacle Detection")]
    [SerializeField]
    private BoxCollider rotationCollisionBox;

    [SerializeField]
    private LayerMask obstacleLayers;

    [SerializeField, Range(1f, 15f)]
    private float sweepAngleStep = 5f;

    [SerializeField, Min(0.01f)]
    private float returnDuration = 0.12f;

    [Header("Puzzle Goal")]
    [SerializeField, Min(0)]
    private int targetStep = 2;

    [SerializeField]
    private bool lockAfterSolved = true;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onPuzzleSolved;

    [SerializeField]
    private UnityEvent onRotationBlocked;

    private Quaternion initialRotation;

    private int currentStep;
    private bool isRotating;
    private bool isSolved;

    private Coroutine rotationCoroutine;

    public int CurrentStep => currentStep;
    public bool IsRotating => isRotating;
    public bool IsSolved => isSolved;

    // --------------------------------
    // Initialize
    // --------------------------------

    private void Awake()
    {
        if (rotatingObject == null)
            rotatingObject = transform;

        initialRotation = rotatingObject.localRotation;

        totalSteps = Mathf.Max(2, totalSteps);

        targetStep = Mathf.Clamp(
            targetStep,
            0,
            totalSteps - 1
        );

        if (rotationCollisionBox == null)
        {
            Debug.LogError(
                "Rotation Collision Box가 지정되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }

        // 충돌 박스는 회전 오브젝트 또는
        // 그 자식에 있어야 한다.
        if (rotationCollisionBox.transform != rotatingObject &&
            !rotationCollisionBox.transform.IsChildOf(rotatingObject))
        {
            Debug.LogError(
                "Rotation Collision Box를 Rotating Object의 " +
                "자식 Collider로 지정해주세요.",
                this
            );

            enabled = false;
            return;
        }

        currentStep = 0;
        isRotating = false;
        isSolved = false;
    }

    // --------------------------------
    // Dash Hit
    // --------------------------------

    // UnityEvent에서 직접 호출
    public void ReceiveDashHit()
    {
        if (dashPlayer == null)
        {
            Debug.LogWarning(
                "Dash Player가 지정되지 않았습니다.",
                this
            );
            return;
        }

        ReceiveDashHitFromPosition(
            dashPlayer.position
        );
    }

    public void ReceiveDashHitFromPosition(
        Vector3 playerWorldPosition)
    {
        if (!isActiveAndEnabled)
            return;

        if (isRotating)
            return;

        if (isSolved && lockAfterSolved)
            return;

        Vector3 localPlayerPosition =
            transform.InverseTransformPoint(
                playerWorldPosition
            );

        float localX = localPlayerPosition.x;

        if (Mathf.Abs(localX) <= centerDeadZone)
            return;

        int direction = localX < 0f ? -1 : 1;

        if (reverseDirection)
            direction *= -1;

        int nextStep = (
            currentStep + direction + totalSteps
        ) % totalSteps;

        rotationCoroutine = StartCoroutine(
            RotateRoutine(nextStep, direction)
        );
    }

    // --------------------------------
    // Rotation
    // --------------------------------

    private IEnumerator RotateRoutine(
        int nextStep,
        int direction)
    {
        isRotating = true;

        Quaternion startRotation =
            rotatingObject.localRotation;

        Quaternion endRotation =
            startRotation *
            Quaternion.Euler(
                0f,
                rotationAngle * direction,
                0f
            );

        float elapsed = 0f;
        float previousT = 0f;

        float duration =
            Mathf.Max(0.01f, rotationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            if (smoothRotation)
                t = t * t * (3f - 2f * t);

            // 이전 프레임에서 이번 프레임까지
            // 실제로 회전할 각도 계산
            float deltaAngle =
                rotationAngle *
                direction *
                (t - previousT);

            // 회전 경로에 장애물이 있는지 검사
            if (IsRotationPathBlocked(deltaAngle))
            {
                onRotationBlocked?.Invoke();

                Debug.Log(
                    "회전 경로에 장애물이 있어 원위치로 복귀합니다.",
                    this
                );

                yield return ReturnToStartRoutine(
                    startRotation
                );

                isRotating = false;
                rotationCoroutine = null;

                // 실패했으므로 CurrentStep 변경 없음
                yield break;
            }

            rotatingObject.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    t
                );

            previousT = t;

            yield return null;
        }

        // 성공한 회전은 정확한 각도로 보정
        rotatingObject.localRotation =
            initialRotation *
            Quaternion.Euler(
                0f,
                nextStep * rotationAngle,
                0f
            );

        currentStep = nextStep;

        isRotating = false;
        rotationCoroutine = null;

        Physics.SyncTransforms();

        CheckPuzzleClear();
    }

    // --------------------------------
    // Obstacle Detection
    // --------------------------------

    // 현재 각도부터 deltaAngle까지
    // 작은 각도로 나눠 충돌 검사
    private bool IsRotationPathBlocked(
        float deltaAngle)
    {
        if (Mathf.Abs(deltaAngle) < 0.0001f)
            return false;

        float maxStep =
            Mathf.Max(1f, sweepAngleStep);

        int sampleCount = Mathf.Max(
            1,
            Mathf.CeilToInt(
                Mathf.Abs(deltaAngle) / maxStep
            )
        );

        Vector3 pivotPosition =
            rotatingObject.position;

        Vector3 axis =
            rotatingObject.TransformDirection(
                Vector3.up
            );

        // 현재 Collider의 중심
        Vector3 boxCenter =
            rotationCollisionBox.transform
                .TransformPoint(
                    rotationCollisionBox.center
                );

        Quaternion boxRotation =
            rotationCollisionBox.transform.rotation;

        Vector3 scale =
            rotationCollisionBox.transform.lossyScale;

        // Collider 크기를 월드 기준으로 계산
        Vector3 halfExtents = Vector3.Scale(
            rotationCollisionBox.size,
            new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z)
            )
        ) * 0.5f;

        // 경계에 살짝 닿는 오검출 감소
        halfExtents *= 0.98f;

        for (int i = 1; i <= sampleCount; i++)
        {
            float angle =
                deltaAngle * i / sampleCount;

            Quaternion deltaRotation =
                Quaternion.AngleAxis(
                    angle,
                    axis
                );

            Vector3 sampleCenter =
                pivotPosition +
                deltaRotation *
                (boxCenter - pivotPosition);

            Quaternion sampleRotation =
                deltaRotation * boxRotation;

            Collider[] hits = Physics.OverlapBox(
                sampleCenter,
                halfExtents,
                sampleRotation,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                // 회전판 자기 자신의 Collider 무시
                if (hit.transform == rotatingObject ||
                    hit.transform.IsChildOf(rotatingObject))
                {
                    continue;
                }

                // 다른 장애물이 회전 경로에 있음
                return true;
            }
        }

        return false;
    }

    // --------------------------------
    // Return Animation
    // --------------------------------

    private IEnumerator ReturnToStartRoutine(
        Quaternion startRotation)
    {
        Quaternion blockedRotation =
            rotatingObject.localRotation;

        float elapsed = 0f;

        float duration =
            Mathf.Max(0.01f, returnDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // 빠르게 돌아오는 애니메이션
            t = t * t * (3f - 2f * t);

            rotatingObject.localRotation =
                Quaternion.Slerp(
                    blockedRotation,
                    startRotation,
                    t
                );

            yield return null;
        }

        rotatingObject.localRotation =
            startRotation;

        Physics.SyncTransforms();
    }

    // --------------------------------
    // Clear Check
    // --------------------------------

    private void CheckPuzzleClear()
    {
        if (isSolved)
            return;

        if (currentStep != targetStep)
            return;

        isSolved = true;

        Debug.Log(
            "돌진 회전판 퍼즐 클리어!",
            this
        );

        onPuzzleSolved?.Invoke();
    }

    // --------------------------------
    // Reset
    // --------------------------------

    public void ResetPuzzle()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }

        rotatingObject.localRotation =
            initialRotation;

        currentStep = 0;
        isRotating = false;
        isSolved = false;

        Physics.SyncTransforms();
    }

    // --------------------------------
    // Editor Test
    // --------------------------------

    [ContextMenu("Test Left Dash")]
    private void TestLeftDash()
    {
        if (!Application.isPlaying)
            return;

        Vector3 leftPosition =
            transform.TransformPoint(
                new Vector3(-2f, 0f, 0f)
            );

        ReceiveDashHitFromPosition(leftPosition);
    }

    [ContextMenu("Test Right Dash")]
    private void TestRightDash()
    {
        if (!Application.isPlaying)
            return;

        Vector3 rightPosition =
            transform.TransformPoint(
                new Vector3(2f, 0f, 0f)
            );

        ReceiveDashHitFromPosition(rightPosition);
    }

    [ContextMenu("Reset Rotation Puzzle")]
    private void TestReset()
    {
        if (Application.isPlaying)
            ResetPuzzle();
    }

    private void OnDisable()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }

        if (rotatingObject != null)
        {
            rotatingObject.localRotation =
                initialRotation *
                Quaternion.Euler(
                    0f,
                    currentStep * rotationAngle,
                    0f
                );
        }

        isRotating = false;
    }
}
