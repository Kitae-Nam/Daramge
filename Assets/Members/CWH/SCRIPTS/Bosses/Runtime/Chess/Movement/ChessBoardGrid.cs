using System;
using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    /// <summary>체스판의 칸 좌표와 킹의 한 칸 경로 탐색만 담당한다. NavMesh/애니메이션은 이동기가 맡는다.</summary>
    public sealed class ChessBoardGrid : MonoBehaviour
    {
        [SerializeField] private Vector3 _center;
        [SerializeField, Min(0.1f)] private float _cellSize = 4.5f;
        [SerializeField, Min(1)] private int _columns = 8;
        [SerializeField, Min(1)] private int _rows = 8;
        public float CellSize => _cellSize;
        public Vector2Int WorldToCell(Vector3 world) => new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt((world.x - _center.x) / _cellSize + (_columns - 1) * 0.5f), 0, _columns - 1),
            Mathf.Clamp(Mathf.RoundToInt((world.z - _center.z) / _cellSize + (_rows - 1) * 0.5f), 0, _rows - 1));
        public Vector3 CellToWorld(Vector2Int cell) => _center + new Vector3(
            (cell.x - (_columns - 1) * 0.5f) * _cellSize, 0f, (cell.y - (_rows - 1) * 0.5f) * _cellSize);
        private bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < _columns && cell.y < _rows;

        /// <summary>BFS로 우회 경로를 찾되 반환값은 다음 한 칸뿐이다. 킹은 항상 인접한 8칸 중 하나로 이동한다.</summary>
        public bool TryFindKingStep(Vector2Int from, Vector2Int target,
            Func<Vector2Int, Vector2Int, bool> canTraverse, out Vector2Int next)
        {
            next = from;
            if (from == target || !Contains(from) || !Contains(target)) return false;
            var queue = new Queue<Vector2Int>();
            var parents = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                // 목표에 가까운 방향부터 검사하되 BFS 깊이는 유지해 최소 이동 횟수를 구한다.
                var neighbors = new List<Vector2Int>(8);
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        if (x != 0 || y != 0) neighbors.Add(current + new Vector2Int(x, y));
                neighbors.Sort((a, b) => (a - target).sqrMagnitude.CompareTo((b - target).sqrMagnitude));
                foreach (Vector2Int cell in neighbors)
                {
                    if (!Contains(cell) || parents.ContainsKey(cell) || !canTraverse(current, cell)) continue;
                    parents[cell] = current;
                    if (cell == target)
                    {
                        next = cell;
                        while (parents[next] != from) next = parents[next];
                        return true;
                    }
                    queue.Enqueue(cell);
                }
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.7f);
            Vector3 corner = _center - new Vector3(_columns * _cellSize, 0f, _rows * _cellSize) * 0.5f;
            for (int x = 0; x <= _columns; x++)
                Gizmos.DrawLine(corner + Vector3.right * (x * _cellSize), corner + Vector3.right * (x * _cellSize) + Vector3.forward * (_rows * _cellSize));
            for (int y = 0; y <= _rows; y++)
                Gizmos.DrawLine(corner + Vector3.forward * (y * _cellSize), corner + Vector3.forward * (y * _cellSize) + Vector3.right * (_columns * _cellSize));
        }
    }
}
