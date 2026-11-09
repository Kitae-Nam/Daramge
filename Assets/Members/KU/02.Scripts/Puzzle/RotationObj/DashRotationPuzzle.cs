
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

    [Header("Puzzle Goal")]
    [SerializeField, Min(0)]
    private int targetStep = 2;

    [SerializeField]
    private bool lockAfterSolved = true;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onPuzzleSolved;

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

        currentStep = 0;
        isRotating = false;
        isSolved = false;
    }

    // --------------------------------
    // Dash Hit
    // --------------------------------

    // UnityEvent에서 직접 호출 가능
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

    // 플레이어의 위치를 전달받아 호출하는 방식도 지원
    public void ReceiveDashHitFromPosition(
        Vector3 playerWorldPosition)
    {
        if (isRotating)
            return;

        if (isSolved && lockAfterSolved)
            return;

        // 회전하지 않는 부모 오브젝트를 기준으로
        // 플레이어의 좌우 위치 계산
        Vector3 localPlayerPosition =
            transform.InverseTransformPoint(
                playerWorldPosition
            );

        float localX = localPlayerPosition.x;

        // 중앙 부근을 맞혔을 때는 회전하지 않음
        if (Mathf.Abs(localX) <= centerDeadZone)
            return;

        // 왼쪽 = -1, 오른쪽 = +1
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

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / rotationDuration
            );

            if (smoothRotation)
            {
                t = t * t * (3f - 2f * t);
            }

            rotatingObject.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    t
                );

            yield return null;
        }

        // 정확한 각도로 보정
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

        CheckPuzzleClear();
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
}
