
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChessPuzzleBoard : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField, Min(1)]
    private int width = 6;

    [SerializeField, Min(1)]
    private int height = 6;

    [SerializeField, Min(0.01f)]
    private float tileSize = 1f;

    [Header("Scene Objects")]
    [SerializeField]
    private List<Transform> blackBlocks = new();

    [SerializeField]
    private List<ChessPullableBlock> whiteBlocks = new();

    [Header("Movement")]
    [SerializeField, Min(0.01f)]
    private float slideDurationPerCell = 0.12f;

    [Header("Puzzle Events")]
    [SerializeField]
    private UnityEvent onPuzzleSolved;

    private readonly HashSet<Vector2Int> blackCells = new();
    private readonly HashSet<Vector2Int> goalCells = new();

    private bool isMoving;
    private bool isSolved;
    private bool isInitialized;

    public bool IsMoving => isMoving;
    public bool IsSolved => isSolved;

    public event Action OnSolved;

    private void Start()
    {
        InitializeBoard();
    }

    // --------------------------------
    // Board Initialization
    // --------------------------------

    private void InitializeBoard()
    {
        blackCells.Clear();
        goalCells.Clear();

        isInitialized = false;
        isMoving = false;
        isSolved = false;

        bool valid = true;

        // 검은 블록 위치 등록
        foreach (Transform blackBlock in blackBlocks)
        {
            if (blackBlock == null)
                continue;

            Vector2Int cell =
                WorldToGrid(blackBlock.position);

            if (!IsInside(cell))
            {
                Debug.LogError(
                    $"{blackBlock.name}이 체스판 밖에 있습니다.",
                    blackBlock
                );

                valid = false;
                continue;
            }

            blackCells.Add(cell);
        }

        HashSet<Vector2Int> occupied = new();

        // 흰 블록 초기화
        foreach (ChessPullableBlock block in whiteBlocks)
        {
            if (block == null)
            {
                valid = false;
                continue;
            }

            block.Initialize(this);

            if (!block.IsInitialized)
            {
                valid = false;
                continue;
            }

            Vector2Int cell = block.GridPosition;

            if (!IsInside(cell) || blackCells.Contains(cell))
            {
                Debug.LogError(
                    $"{block.name}의 시작 위치가 잘못되었습니다.",
                    block
                );

                valid = false;
            }

            if (!occupied.Add(cell))
            {
                Debug.LogError(
                    $"흰 블록이 {cell} 위치에서 겹칩니다.",
                    block
                );

                valid = false;
            }

            // 흰 블록에 등록된 모든 목표를 수집
            foreach (Vector2Int goal in block.GoalPositions)
            {
                if (blackCells.Contains(goal))
                {
                    Debug.LogError(
                        $"목표 지점 {goal}에 검은 블록이 있습니다.",
                        this
                    );

                    valid = false;
                }

                goalCells.Add(goal);
            }
        }

        // 현재 퍼즐은 모든 목표에 블록 하나씩 배치
        if (goalCells.Count != whiteBlocks.Count)
        {
            Debug.LogError(
                "목표 타일의 개수와 흰 블록의 개수가 다릅니다. " +
                $"목표: {goalCells.Count}, " +
                $"흰 블록: {whiteBlocks.Count}",
                this
            );

            valid = false;
        }

        isInitialized = valid;

        if (isInitialized)
            CheckPuzzleClear();
    }

    // --------------------------------
    // Grid Coordinate
    // --------------------------------

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 local =
            transform.InverseTransformPoint(worldPosition);

        int x = Mathf.RoundToInt(local.x / tileSize);
        int y = Mathf.RoundToInt(local.z / tileSize);

        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorld(
        Vector2Int cell,
        float localHeight)
    {
        Vector3 localPosition = new Vector3(
            cell.x * tileSize,
            localHeight,
            cell.y * tileSize
        );

        return transform.TransformPoint(localPosition);
    }

    public float GetLocalHeight(Vector3 worldPosition)
    {
        return transform
            .InverseTransformPoint(worldPosition).y;
    }

    // --------------------------------
    // Board Boundary
    // --------------------------------

    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 &&
               cell.x < width &&
               cell.y >= 0 &&
               cell.y < height;
    }

    public bool IsWorldPositionInsideBoard(
        Vector3 worldPosition)
    {
        Vector3 local =
            transform.InverseTransformPoint(worldPosition);

        float minX = -tileSize * 0.5f;
        float maxX = (width - 0.5f) * tileSize;

        float minZ = -tileSize * 0.5f;
        float maxZ = (height - 0.5f) * tileSize;

        return local.x >= minX &&
               local.x <= maxX &&
               local.z >= minZ &&
               local.z <= maxZ;
    }

    // --------------------------------
    // Obstacle Detection
    // --------------------------------

    private bool IsBlocked(
        Vector2Int cell,
        ChessPullableBlock movingBlock)
    {
        if (!IsInside(cell))
            return true;

        if (blackCells.Contains(cell))
            return true;

        foreach (ChessPullableBlock block in whiteBlocks)
        {
            if (block == null || block == movingBlock)
                continue;

            if (block.GridPosition == cell)
                return true;
        }

        return false;
    }

    // --------------------------------
    // Pull Block
    // --------------------------------

    public bool TryPullBlock(
        ChessPullableBlock block,
        Vector3 playerWorldPosition)
    {
        if (!isInitialized || isMoving || isSolved)
            return false;

        if (block == null || !whiteBlocks.Contains(block))
            return false;

        // 판 위에서는 당길 수 없음
        if (IsWorldPositionInsideBoard(playerWorldPosition))
            return false;

        Vector2Int direction = CalculatePullDirection(
            block.transform.position,
            playerWorldPosition
        );

        if (direction == Vector2Int.zero)
            return false;

        Vector2Int startCell = block.GridPosition;
        Vector2Int destination = startCell;

        // 막힐 때까지 이동
        while (true)
        {
            Vector2Int next = destination + direction;

            if (IsBlocked(next, block))
                break;

            destination = next;
        }

        if (destination == startCell)
            return false;

        int distance =
            Mathf.Abs(destination.x - startCell.x) +
            Mathf.Abs(destination.y - startCell.y);

        float duration =
            distance * slideDurationPerCell;

        isMoving = true;

        block.SlideTo(
            destination,
            duration,
            OnBlockMovementFinished
        );

        return true;
    }

    // --------------------------------
    // Pull Direction
    // --------------------------------

    private Vector2Int CalculatePullDirection(
        Vector3 blockWorldPosition,
        Vector3 playerWorldPosition)
    {
        Vector3 blockLocal =
            transform.InverseTransformPoint(blockWorldPosition);

        Vector3 playerLocal =
            transform.InverseTransformPoint(playerWorldPosition);

        Vector3 delta = playerLocal - blockLocal;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
        {
            return new Vector2Int(
                delta.x >= 0f ? 1 : -1,
                0
            );
        }

        if (Mathf.Abs(delta.z) > 0.001f)
        {
            return new Vector2Int(
                0,
                delta.z >= 0f ? 1 : -1
            );
        }

        return Vector2Int.zero;
    }

    // --------------------------------
    // Puzzle Complete
    // --------------------------------

    private void OnBlockMovementFinished()
    {
        isMoving = false;
        CheckPuzzleClear();
    }

    public void CheckPuzzleClear()
    {
        if (!isInitialized || isMoving || isSolved)
            return;

        if (goalCells.Count == 0)
            return;

        // 현재 흰 블록들이 차지한 모든 칸
        HashSet<Vector2Int> occupiedCells = new();

        foreach (ChessPullableBlock block in whiteBlocks)
        {
            if (block == null || !block.IsInitialized)
                return;

            occupiedCells.Add(block.GridPosition);
        }

        // 목표 칸이 모두 흰 블록으로 채워졌는지 검사
        foreach (Vector2Int goal in goalCells)
        {
            if (!occupiedCells.Contains(goal))
                return;
        }

        isSolved = true;

        Debug.Log("체스판 퍼즐 클리어!");

        onPuzzleSolved?.Invoke();
        OnSolved?.Invoke();
    }

    // --------------------------------
    // Reset
    // --------------------------------

    public void ResetPuzzle()
    {
        if (!isInitialized)
            return;

        isMoving = false;
        isSolved = false;

        foreach (ChessPullableBlock block in whiteBlocks)
        {
            if (block != null)
                block.ResetBlock();
        }

        Physics.SyncTransforms();

        CheckPuzzleClear();
    }

    // --------------------------------
    // Gizmos
    // --------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        for (int x = 0; x <= width; x++)
        {
            float localX = (x - 0.5f) * tileSize;

            Vector3 start = transform.TransformPoint(
                new Vector3(
                    localX,
                    0.05f,
                    -0.5f * tileSize
                )
            );

            Vector3 end = transform.TransformPoint(
                new Vector3(
                    localX,
                    0.05f,
                    (height - 0.5f) * tileSize
                )
            );

            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= height; y++)
        {
            float localZ = (y - 0.5f) * tileSize;

            Vector3 start = transform.TransformPoint(
                new Vector3(
                    -0.5f * tileSize,
                    0.05f,
                    localZ
                )
            );

            Vector3 end = transform.TransformPoint(
                new Vector3(
                    (width - 0.5f) * tileSize,
                    0.05f,
                    localZ
                )
            );

            Gizmos.DrawLine(start, end);
        }
    }
}
