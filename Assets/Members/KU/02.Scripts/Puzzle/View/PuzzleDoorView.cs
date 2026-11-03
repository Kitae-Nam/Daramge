
using System.Collections;
using UnityEngine;

public class PuzzleDoorView : MonoBehaviour
{
    public enum AxisOrder
    {
        XYZ,
        XZY,
        YXZ,
        YZX,
        ZXY,
        ZYX
    }

    public enum AnimationOrder
    {
        RotationFirst,
        MovementFirst,
        Simultaneous
    }

    [Header("Movement")]
    [SerializeField]
    private Vector3 openOffset = new Vector3(0f, 3f, 0f);

    [SerializeField]
    private AxisOrder moveOrder = AxisOrder.YXZ;

    [SerializeField, Min(0.01f)]
    private float moveSpeed = 2f;

    [SerializeField]
    private bool smoothMovement = true;

    [Header("Rotation")]
    [SerializeField]
    private Vector3 rotationOffset = Vector3.zero;

    [SerializeField, Min(0.01f)]
    private float rotationSpeed = 90f;

    [SerializeField]
    private bool smoothRotation = true;

    [Header("Animation")]
    [SerializeField]
    private AnimationOrder animationOrder =
        AnimationOrder.Simultaneous;

    private Vector3 closedPosition;
    private Vector3 openedPosition;

    private Quaternion closedRotation;
    private Quaternion openedRotation;

    private Coroutine animationCoroutine;
    private Coroutine movementCoroutine;
    private Coroutine rotationCoroutine;

    private bool isOpen;
    private bool isInitialized;

    private bool movementFinished;
    private bool rotationFinished;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        // 씬에 배치한 위치와 회전을 닫힌 상태로 저장
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;

        // 열린 위치
        openedPosition = closedPosition + openOffset;

        // 열린 회전
        openedRotation =
            closedRotation *
            Quaternion.Euler(rotationOffset);

        isOpen = false;
        isInitialized = true;

        transform.localPosition = closedPosition;
        transform.localRotation = closedRotation;
    }

    // --------------------------------
    // Public Control
    // --------------------------------

    public void SetOpen(bool open)
    {
        if (!isInitialized)
            return;

        // 이미 해당 상태를 목표로 움직이고 있다면 무시
        if (isOpen == open)
            return;

        isOpen = open;

        // 기존 애니메이션 정지
        StopAllDoorAnimations();

        // 현재 위치와 회전에서 새로운 목표로 이동
        animationCoroutine = StartCoroutine(
            DoorAnimationRoutine(open)
        );
    }

    // --------------------------------
    // Main Animation
    // --------------------------------

    private IEnumerator DoorAnimationRoutine(bool opening)
    {
        movementFinished = false;
        rotationFinished = false;

        switch (animationOrder)
        {
            case AnimationOrder.RotationFirst:

                if (opening)
                {
                    // 열기: 회전 → 이동
                    yield return RotateDoorRoutine(opening);
                    yield return MoveDoorRoutine(opening);
                }
                else
                {
                    // 닫기: 이동 → 회전
                    yield return MoveDoorRoutine(opening);
                    yield return RotateDoorRoutine(opening);
                }

                break;

            case AnimationOrder.MovementFirst:

                if (opening)
                {
                    // 열기: 이동 → 회전
                    yield return MoveDoorRoutine(opening);
                    yield return RotateDoorRoutine(opening);
                }
                else
                {
                    // 닫기: 회전 → 이동
                    yield return RotateDoorRoutine(opening);
                    yield return MoveDoorRoutine(opening);
                }

                break;

            case AnimationOrder.Simultaneous:

                // 이동과 회전을 동시에 실행
                movementCoroutine = StartCoroutine(
                    MoveDoorRoutine(opening)
                );

                rotationCoroutine = StartCoroutine(
                    RotateDoorRoutine(opening)
                );

                // 두 동작이 모두 끝날 때까지 대기
                yield return new WaitUntil(
                    () => movementFinished && rotationFinished
                );

                break;
        }

        // 최종 위치와 회전 정확하게 보정
        transform.localPosition =
            opening ? openedPosition : closedPosition;

        transform.localRotation =
            opening ? openedRotation : closedRotation;

        movementCoroutine = null;
        rotationCoroutine = null;
        animationCoroutine = null;
    }

    // --------------------------------
    // Movement
    // --------------------------------

    private IEnumerator MoveDoorRoutine(bool opening)
    {
        Vector3 targetPosition =
            opening ? openedPosition : closedPosition;

        int[] axisSequence = GetAxisSequence();

        // 닫힐 때는 축 이동 순서를 반대로
        if (!opening)
        {
            System.Array.Reverse(axisSequence);
        }

        foreach (int axis in axisSequence)
        {
            yield return MoveAlongAxis(
                axis,
                targetPosition[axis]
            );
        }

        transform.localPosition = targetPosition;
        movementFinished = true;
    }

    private IEnumerator MoveAlongAxis(
        int axis,
        float targetValue
    )
    {
        Vector3 startPosition = transform.localPosition;

        float startValue = startPosition[axis];

        float distance = Mathf.Abs(
            targetValue - startValue
        );

        // 이동량이 0이면 건너뛰기
        if (distance < 0.0001f)
            yield break;

        float duration = distance / Mathf.Max(
            moveSpeed,
            0.01f
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            if (smoothMovement)
            {
                // Ease In / Ease Out
                t = t * t * (3f - 2f * t);
            }

            Vector3 currentPosition =
                transform.localPosition;

            currentPosition[axis] = Mathf.Lerp(
                startValue,
                targetValue,
                t
            );

            transform.localPosition = currentPosition;

            yield return null;
        }

        Vector3 finalPosition = transform.localPosition;
        finalPosition[axis] = targetValue;

        transform.localPosition = finalPosition;
    }

    // --------------------------------
    // Rotation
    // --------------------------------

    private IEnumerator RotateDoorRoutine(bool opening)
    {
        Quaternion targetRotation =
            opening ? openedRotation : closedRotation;

        Quaternion startRotation =
            transform.localRotation;

        float angle = Quaternion.Angle(
            startRotation,
            targetRotation
        );

        if (angle < 0.01f)
        {
            transform.localRotation = targetRotation;
            rotationFinished = true;
            yield break;
        }

        float duration = angle / Mathf.Max(
            rotationSpeed,
            0.01f
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            if (smoothRotation)
            {
                // Ease In / Ease Out
                t = t * t * (3f - 2f * t);
            }

            transform.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        transform.localRotation = targetRotation;
        rotationFinished = true;
    }

    // --------------------------------
    // Stop Animation
    // --------------------------------

    private void StopAllDoorAnimations()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }

        movementFinished = false;
        rotationFinished = false;
    }

    private void OnDisable()
    {
        StopAllDoorAnimations();
    }

    // --------------------------------
    // Axis Sequence
    // --------------------------------

    private int[] GetAxisSequence()
    {
        switch (moveOrder)
        {
            case AxisOrder.XYZ:
                return new int[] { 0, 1, 2 };

            case AxisOrder.XZY:
                return new int[] { 0, 2, 1 };

            case AxisOrder.YXZ:
                return new int[] { 1, 0, 2 };

            case AxisOrder.YZX:
                return new int[] { 1, 2, 0 };

            case AxisOrder.ZXY:
                return new int[] { 2, 0, 1 };

            case AxisOrder.ZYX:
                return new int[] { 2, 1, 0 };

            default:
                return new int[] { 0, 1, 2 };
        }
    }

    // --------------------------------
    // Gizmos
    // --------------------------------

    private void OnDrawGizmosSelected()
    {
        Vector3 closedWorld;
        Vector3 openedWorld;

        if (Application.isPlaying && isInitialized)
        {
            closedWorld = transform.parent != null
                ? transform.parent.TransformPoint(closedPosition)
                : closedPosition;

            openedWorld = transform.parent != null
                ? transform.parent.TransformPoint(openedPosition)
                : openedPosition;
        }
        else
        {
            closedWorld = transform.position;

            openedWorld = transform.parent != null
                ? closedWorld +
                  transform.parent.TransformVector(openOffset)
                : closedWorld + openOffset;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(closedWorld, 0.15f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(openedWorld, 0.15f);

        Gizmos.DrawLine(closedWorld, openedWorld);
    }
}
