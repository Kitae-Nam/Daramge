
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

    [Header("Door Movement")]
    [SerializeField]
    private Vector3 openOffset = new Vector3(0f, 3f, 0f);

    [SerializeField]
    private AxisOrder moveOrder = AxisOrder.YXZ;

    [Header("Movement Settings")]
    [SerializeField, Min(0.01f)]
    private float moveSpeed = 2f;

    [SerializeField]
    private bool smoothMovement = true;

    private Vector3 closedPosition;
    private Vector3 openedPosition;

    private Coroutine moveCoroutine;

    private bool isOpen;
    private bool isInitialized;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        // 씬에 배치된 위치를 닫힌 위치로 저장
        closedPosition = transform.localPosition;

        // Inspector에서 지정한 Offset으로 열린 위치 계산
        openedPosition = closedPosition + openOffset;

        isOpen = false;
        isInitialized = true;

        transform.localPosition = closedPosition;
    }

    public void SetOpen(bool open)
    {
        if (!isInitialized)
            return;

        // 같은 목표 상태라면 중복 실행하지 않음
        if (isOpen == open)
            return;

        isOpen = open;

        // 기존 문 이동 중단
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        // 현재 위치에서 새로운 목표까지 이동
        moveCoroutine = StartCoroutine(
            MoveDoorRoutine(open)
        );
    }

    private IEnumerator MoveDoorRoutine(bool opening)
    {
        Vector3 targetPosition =
            opening ? openedPosition : closedPosition;

        int[] axisSequence = GetAxisSequence();

        // 닫힐 때는 열릴 때의 반대 순서로 이동
        if (!opening)
        {
            System.Array.Reverse(axisSequence);
        }

        // 축마다 순차적으로 이동
        foreach (int axis in axisSequence)
        {
            yield return MoveAlongAxis(
                axis,
                targetPosition
            );
        }

        // 최종 위치 보정
        transform.localPosition = targetPosition;

        moveCoroutine = null;
    }

    private IEnumerator MoveAlongAxis(
        int axis,
        Vector3 targetPosition
    )
    {
        Vector3 startPosition = transform.localPosition;

        float startValue = startPosition[axis];
        float targetValue = targetPosition[axis];

        float distance = Mathf.Abs(
            targetValue - startValue
        );

        // 이동 거리가 없다면 해당 축 건너뛰기
        if (distance < 0.0001f)
            yield break;

        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            if (smoothMovement)
            {
                // Ease In / Ease Out
                t = t * t * (3f - 2f * t);
            }

            Vector3 currentPosition =
                transform.localPosition;

            // 현재 이동 중인 축만 변경
            currentPosition[axis] = Mathf.Lerp(
                startValue,
                targetValue,
                t
            );

            transform.localPosition = currentPosition;

            yield return null;
        }

        // 해당 축의 목표 좌표 정확하게 맞추기
        Vector3 finalPosition = transform.localPosition;

        finalPosition[axis] = targetValue;

        transform.localPosition = finalPosition;
    }

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

    private void OnDrawGizmosSelected()
    {
        // 닫힌 위치와 열린 위치를 Scene 뷰에 표시
        Vector3 closedWorldPosition;
        Vector3 openedWorldPosition;

        if (Application.isPlaying && isInitialized)
        {
            closedWorldPosition =
                transform.parent != null
                    ? transform.parent.TransformPoint(closedPosition)
                    : closedPosition;

            openedWorldPosition =
                transform.parent != null
                    ? transform.parent.TransformPoint(openedPosition)
                    : openedPosition;
        }
        else
        {
            closedWorldPosition = transform.position;

            openedWorldPosition =
                transform.parent != null
                    ? transform.parent.TransformVector(openOffset)
                      + closedWorldPosition
                    : closedWorldPosition + openOffset;
        }

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            closedWorldPosition,
            0.15f
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            openedWorldPosition,
            0.15f
        );

        Gizmos.DrawLine(
            closedWorldPosition,
            openedWorldPosition
        );
    }
}
