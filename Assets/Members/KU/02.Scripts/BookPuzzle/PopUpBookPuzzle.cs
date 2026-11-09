
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PopUpBookPuzzle : MonoBehaviour
{
    [Header("Book Pages")]
    [SerializeField]
    private Transform[] pagePivots;

    [Header("Page Turn Settings")]
    [SerializeField]
    private float pageTurnAngle = 90f;

    [SerializeField, Min(0.01f)]
    private float pageTurnDuration = 0.6f;

    [SerializeField]
    private bool smoothRotation = true;

    [Header("Tongue Hitbox")]
    [SerializeField]
    private Collider tongueHitbox;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onPageTurned;

    [SerializeField]
    private UnityEvent onPuzzleSolved;

    private Quaternion[] initialRotations;

    private int currentPageIndex;

    private bool isTurning;
    private bool isSolved;
    private bool isInitialized;

    private Coroutine turnCoroutine;

    public int CurrentPageIndex => currentPageIndex;
    public bool IsTurning => isTurning;
    public bool IsSolved => isSolved;

    // --------------------------------
    // Initialize
    // --------------------------------

    private void Awake()
    {
        if (pagePivots == null ||
            pagePivots.Length == 0)
        {
            Debug.LogError(
                "책 페이지가 등록되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }

        initialRotations =
            new Quaternion[pagePivots.Length];

        for (int i = 0; i < pagePivots.Length; i++)
        {
            if (pagePivots[i] == null)
            {
                Debug.LogError(
                    $"Page {i}가 비어 있습니다.",
                    this
                );

                enabled = false;
                return;
            }

            initialRotations[i] =
                pagePivots[i].localRotation;
        }

        currentPageIndex = 0;
        isTurning = false;
        isSolved = false;
        isInitialized = true;
    }

    // --------------------------------
    // Tongue Interaction
    // --------------------------------

    // UnityEvent에서 직접 호출 가능
    public void ReceiveTongueHit()
    {
        if (!isInitialized)
            return;

        if (!isActiveAndEnabled)
            return;

        if (isTurning || isSolved)
            return;

        if (currentPageIndex >= pagePivots.Length)
            return;

        turnCoroutine = StartCoroutine(
            TurnPageRoutine(currentPageIndex)
        );
    }

    // --------------------------------
    // Page Turn
    // --------------------------------

    private IEnumerator TurnPageRoutine(int pageIndex)
    {
        isTurning = true;

        Transform page = pagePivots[pageIndex];

        Quaternion startRotation =
            initialRotations[pageIndex];

        Quaternion targetRotation =
            startRotation *
            Quaternion.Euler(
                pageTurnAngle,
                0f,
                0f
            );

        float elapsed = 0f;

        while (elapsed < pageTurnDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / pageTurnDuration
            );

            if (smoothRotation)
            {
                t = t * t * (3f - 2f * t);
            }

            page.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        page.localRotation = targetRotation;

        currentPageIndex++;

        isTurning = false;
        turnCoroutine = null;

        onPageTurned?.Invoke();

        CheckPuzzleClear();
    }

    // --------------------------------
    // Puzzle Clear
    // --------------------------------

    private void CheckPuzzleClear()
    {
        if (isSolved)
            return;

        if (currentPageIndex < pagePivots.Length)
            return;

        isSolved = true;

        Debug.Log(
            "팝업북 퍼즐 클리어!",
            this
        );

        if (tongueHitbox != null)
            tongueHitbox.enabled = false;

        onPuzzleSolved?.Invoke();
    }

    // --------------------------------
    // Reset
    // --------------------------------

    public void ResetPuzzle()
    {
        if (!isInitialized)
            return;

        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
            turnCoroutine = null;
        }

        for (int i = 0; i < pagePivots.Length; i++)
        {
            if (pagePivots[i] == null)
                continue;

            pagePivots[i].localRotation =
                initialRotations[i];
        }

        currentPageIndex = 0;
        isTurning = false;
        isSolved = false;

        if (tongueHitbox != null)
            tongueHitbox.enabled = true;
    }

    // --------------------------------
    // Editor Test
    // --------------------------------

    [ContextMenu("Test Turn Page")]
    private void TestTurnPage()
    {
        if (Application.isPlaying)
            ReceiveTongueHit();
    }

    [ContextMenu("Reset Book Puzzle")]
    private void TestResetPuzzle()
    {
        if (Application.isPlaying)
            ResetPuzzle();
    }

    private void OnDisable()
    {
        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
            turnCoroutine = null;

            // 중간에 회전이 멈추면 해당 페이지 복구
            if (isInitialized &&
                currentPageIndex < pagePivots.Length &&
                pagePivots[currentPageIndex] != null)
            {
                pagePivots[currentPageIndex].localRotation =
                    initialRotations[currentPageIndex];
            }
        }

        isTurning = false;
    }
}
