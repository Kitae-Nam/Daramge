
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChessPullableBlock : MonoBehaviour
{
    [Header("Goal Tiles")]
    [SerializeField]
    private List<Transform> goalTiles = new();

    private readonly HashSet<Vector2Int> goalPositions = new();

    private ChessPuzzleBoard board;

    private Vector2Int startPosition;
    private Vector2Int gridPosition;

    private float localHeight;

    private Coroutine movementCoroutine;

    private bool isInitialized;
    private bool isSliding;

    public Vector2Int GridPosition => gridPosition;

    public bool IsInitialized => isInitialized;
    public bool IsSliding => isSliding;

    public IReadOnlyCollection<Vector2Int> GoalPositions
        => goalPositions;

    public bool IsOnGoal =>
        goalPositions.Contains(gridPosition);

    // --------------------------------
    // Initialize
    // --------------------------------

    public void Initialize(ChessPuzzleBoard puzzleBoard)
    {
        board = puzzleBoard;

        goalPositions.Clear();

        if (goalTiles == null || goalTiles.Count == 0)
        {
            Debug.LogError(
                $"{name}에 Goal Tiles가 지정되지 않았습니다.",
                this
            );

            isInitialized = false;
            return;
        }

        // 현재 배치된 위치를 시작 위치로 사용
        startPosition =
            board.WorldToGrid(transform.position);

        gridPosition = startPosition;

        // 모든 목표 타일의 위치를 등록
        foreach (Transform goalTile in goalTiles)
        {
            if (goalTile == null)
                continue;

            Vector2Int goalPosition =
                board.WorldToGrid(goalTile.position);

            if (!board.IsInside(goalPosition))
            {
                Debug.LogWarning(
                    $"{goalTile.name}이 체스판 밖에 있습니다.",
                    goalTile
                );

                continue;
            }

            goalPositions.Add(goalPosition);
        }

        if (goalPositions.Count == 0)
        {
            Debug.LogError(
                $"{name}에 유효한 목표 타일이 없습니다.",
                this
            );

            isInitialized = false;
            return;
        }

        localHeight =
            board.GetLocalHeight(transform.position);

        isInitialized = true;
        isSliding = false;

        transform.position =
            board.GridToWorld(gridPosition, localHeight);
    }

    // --------------------------------
    // Goal Check
    // --------------------------------

    public bool IsAtGoal(Vector2Int cell)
    {
        return goalPositions.Contains(cell);
    }

    // --------------------------------
    // Slide
    // --------------------------------

    public void SlideTo(
        Vector2Int targetCell,
        float duration,
        Action onFinished)
    {
        if (!isInitialized || isSliding)
            return;

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        movementCoroutine = StartCoroutine(
            SlideRoutine(targetCell, duration, onFinished)
        );
    }

    private IEnumerator SlideRoutine(
        Vector2Int targetCell,
        float duration,
        Action onFinished)
    {
        isSliding = true;

        Vector3 startWorldPosition =
            transform.position;

        Vector3 targetWorldPosition =
            board.GridToWorld(targetCell, localHeight);

        float elapsed = 0f;

        duration = Mathf.Max(duration, 0.01f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT =
                t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(
                startWorldPosition,
                targetWorldPosition,
                smoothT
            );

            yield return null;
        }

        transform.position = targetWorldPosition;

        gridPosition = targetCell;

        Physics.SyncTransforms();

        isSliding = false;
        movementCoroutine = null;

        onFinished?.Invoke();
    }

    // --------------------------------
    // Reset
    // --------------------------------

    public void ResetBlock()
    {
        if (!isInitialized)
            return;

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        gridPosition = startPosition;
        isSliding = false;

        transform.position =
            board.GridToWorld(startPosition, localHeight);

        Physics.SyncTransforms();
    }

    // --------------------------------
    // Gizmos
    // --------------------------------

    private void OnDrawGizmosSelected()
    {
        if (goalTiles == null)
            return;

        Gizmos.color = Color.green;

        foreach (Transform goalTile in goalTiles)
        {
            if (goalTile == null)
                continue;

            Gizmos.DrawLine(
                transform.position,
                goalTile.position
            );

            Gizmos.DrawWireSphere(
                goalTile.position,
                0.2f
            );
        }
    }
}
