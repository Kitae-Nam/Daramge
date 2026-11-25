using System;
using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime
{
    public enum ChessColossusMoveKind { Rook, Knight, Bishop }

    public readonly struct ChessColossusMove
    {
        public ChessColossusMoveKind Kind { get; }
        public Vector2Int FirstWaypoint { get; }
        public Vector2Int Destination { get; }

        public ChessColossusMove(ChessColossusMoveKind kind, Vector2Int firstWaypoint, Vector2Int destination)
        {
            Kind = kind;
            FirstWaypoint = firstWaypoint;
            Destination = destination;
        }
    }

    /// <summary>체스 이동 규칙과 경로 선택만 계산한다. 통과 가능 여부는 호출자가 제공한다.</summary>
    public static class ChessColossusMovePlanner
    {
        private static readonly Vector2Int[] Orthogonal = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        private static readonly Vector2Int[] Diagonal = { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1) };

        public static bool TryPlan(ChessBoardGrid board, Vector2Int from, Vector2Int target,
            Predicate<Vector2Int> canStop, Func<Vector2Int, Vector2Int, bool> canTraverse, int maxSlide,
            ChessColossusMoveKind preferred, out ChessColossusMove next)
        {
            next = default;
            if (!board.Contains(from)) return false;
            var visited = new HashSet<Vector2Int> { from };
            var queue = new Queue<(Vector2Int Cell, ChessColossusMove FirstMove)>();
            queue.Enqueue((from, default));
            int closest = (from - target).sqrMagnitude;
            bool foundCloser = false;
            var candidates = new List<ChessColossusMove>();

            // 한 번의 이동을 기준으로 BFS를 하므로, 첫 이동에서 멀어져야 하는 우회로도 찾는다.
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Cell != from && canStop(current.Cell))
                {
                    next = current.FirstMove;
                    return true;
                }
                int distance = (current.Cell - target).sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    next = current.FirstMove;
                    foundCloser = true;
                }

                candidates.Clear();
                FindSlides(candidates, board, current.Cell, Orthogonal, ChessColossusMoveKind.Rook, maxSlide, canTraverse);
                FindSlides(candidates, board, current.Cell, Diagonal, ChessColossusMoveKind.Bishop, maxSlide, canTraverse);
                FindKnights(candidates, board, current.Cell, canTraverse);
                if (current.Cell == from) candidates.Sort((a, b) => Compare(a, b, preferred, target));
                foreach (var move in candidates)
                    if (visited.Add(move.Destination))
                        queue.Enqueue((move.Destination, current.Cell == from ? move : current.FirstMove));
            }
            return foundCloser;
        }

        private static int Compare(ChessColossusMove a, ChessColossusMove b, ChessColossusMoveKind preferred, Vector2Int target)
        {
            int rankA = ((int)a.Kind - (int)preferred + 3) % 3;
            int rankB = ((int)b.Kind - (int)preferred + 3) % 3;
            int order = rankA.CompareTo(rankB);
            if (order != 0) return order;
            order = (a.Destination - target).sqrMagnitude.CompareTo((b.Destination - target).sqrMagnitude);
            if (order != 0) return order;
            order = a.Destination.x.CompareTo(b.Destination.x);
            return order != 0 ? order : a.Destination.y.CompareTo(b.Destination.y);
        }

        private static void FindSlides(List<ChessColossusMove> candidates, ChessBoardGrid board, Vector2Int from,
            Vector2Int[] directions, ChessColossusMoveKind kind, int maxSlide, Func<Vector2Int, Vector2Int, bool> canTraverse)
        {
            foreach (Vector2Int direction in directions)
            {
                Vector2Int previous = from;
                for (int steps = 0; steps < maxSlide; steps++)
                {
                    Vector2Int next = previous + direction;
                    if (!board.Contains(next) || !canTraverse(previous, next)) break;
                    candidates.Add(new ChessColossusMove(kind, next, next));
                    previous = next;
                }
            }
        }

        private static void FindKnights(List<ChessColossusMove> candidates, ChessBoardGrid board, Vector2Int from,
            Func<Vector2Int, Vector2Int, bool> canTraverse)
        {
            foreach (Vector2Int major in Orthogonal)
            {
                var perpendicular = new Vector2Int(-major.y, major.x);
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector2Int minor = perpendicular * sign;
                    Vector2Int destination = from + 2 * major + minor;
                    if (!board.Contains(destination)) continue;

                    // 거신병은 점프 대신 지상에서 2+1 또는 1+2로 꺾는다. 중간 칸도 건너뛸 수 없다.
                    Vector2Int corner = from + 2 * major;
                    if (CanTrace(board, from, from + major, corner, destination, canTraverse))
                        candidates.Add(new ChessColossusMove(ChessColossusMoveKind.Knight, corner, destination));
                    else if (CanTrace(board, from, from + minor, from + minor + major, destination, canTraverse))
                        candidates.Add(new ChessColossusMove(ChessColossusMoveKind.Knight, from + minor, destination));
                }
            }
        }

        private static bool CanTrace(ChessBoardGrid board, Vector2Int from, Vector2Int first, Vector2Int second, Vector2Int last,
            Func<Vector2Int, Vector2Int, bool> canTraverse) => board.Contains(first) && board.Contains(second) &&
            canTraverse(from, first) && canTraverse(first, second) && canTraverse(second, last);
    }
}
