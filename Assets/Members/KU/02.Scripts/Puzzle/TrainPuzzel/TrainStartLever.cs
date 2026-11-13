
using System.Collections;
using UnityEngine;

public class TrainStartLever : MonoBehaviour
{
    [Header("Puzzle Manager")]
    [SerializeField]
    private TrainTrackPuzzleManager puzzleManager;

    [Header("Lever Visual")]
    [SerializeField]
    private Transform leverHandle;

    [SerializeField]
    private Vector3 pullRotation =
        new Vector3(45f, 0f, 0f);

    [SerializeField, Min(0.01f)]
    private float pullDuration = 0.15f;

    [SerializeField, Min(0.01f)]
    private float returnDuration = 0.25f;

    private Quaternion initialRotation;
    private Coroutine leverCoroutine;

    private void Awake()
    {
        if (leverHandle != null)
            initialRotation = leverHandle.localRotation;
    }

    // 개구리 혀 적중 시 호출
    // UnityEvent 연결 가능
    public void ReceiveTongueHit()
    {
        if (puzzleManager == null)
            return;

        // 출발 가능한 상태인지 매니저에서 검사
        if (!puzzleManager.RequestTrainDeparture())
            return;

        if (leverHandle == null)
            return;

        if (leverCoroutine != null)
            StopCoroutine(leverCoroutine);

        leverCoroutine = StartCoroutine(
            PullLeverRoutine()
        );
    }

    private IEnumerator PullLeverRoutine()
    {
        Quaternion pulledRotation =
            initialRotation *
            Quaternion.Euler(pullRotation);

        yield return RotateLever(
            leverHandle.localRotation,
            pulledRotation,
            pullDuration
        );

        yield return RotateLever(
            pulledRotation,
            initialRotation,
            returnDuration
        );

        leverCoroutine = null;
    }

    private IEnumerator RotateLever(
        Quaternion from,
        Quaternion to,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            t = t * t * (3f - 2f * t);

            leverHandle.localRotation =
                Quaternion.Slerp(from, to, t);

            yield return null;
        }

        leverHandle.localRotation = to;
    }

    [ContextMenu("Test Pull Lever")]
    private void TestPullLever()
    {
        if (Application.isPlaying)
            ReceiveTongueHit();
    }

    private void OnDisable()
    {
        if (leverCoroutine != null)
        {
            StopCoroutine(leverCoroutine);
            leverCoroutine = null;
        }

        if (leverHandle != null)
            leverHandle.localRotation = initialRotation;
    }
}
