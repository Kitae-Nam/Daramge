using System.Collections.Generic;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons
{
    /// <summary>예고, 충격파 연출, 피해가 같은 칸 목록을 사용하도록 공격 도형을 만든다.</summary>
    public static class ChessColossusAttackPattern
    {
        public enum JumpPiece { Rook, Bishop, Knight }

        private static readonly Vector2Int[] Axes = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private static readonly Vector2Int[] Diagonals = { new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };

        public static Vector2Int Facing(Vector3 direction) => Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
            ? (direction.x >= 0 ? Vector2Int.right : Vector2Int.left)
            : (direction.z >= 0 ? Vector2Int.up : Vector2Int.down);

        public static List<Vector2Int> Rectangle(ChessBoardGrid board, Vector2Int origin, Vector2Int forward,
            int width, int depth, int firstRow)
        {
            var cells = new List<Vector2Int>(width * depth);
            var right = new Vector2Int(forward.y, -forward.x);
            for (int row = firstRow; row < firstRow + depth; row++)
            for (int column = 0; column < width; column++)
                Add(board, cells, origin + forward * row + right * (column - width / 2));
            return cells;
        }

        public static List<Vector2Int> Landing(ChessBoardGrid board, Vector2Int center) => Rectangle(board, center, Vector2Int.up, 3, 3, -1);

        public static List<Vector2Int> JumpFollowUp(ChessBoardGrid board, Vector2Int center, JumpPiece kind, int ring)
        {
            var cells = new List<Vector2Int>(8);
            if (kind == JumpPiece.Knight)
            {
                if (ring != 2) return cells;
                foreach (var axis in Axes)
                {
                    var side = new Vector2Int(axis.y, -axis.x);
                    Add(board, cells, center + 2 * axis + side);
                    Add(board, cells, center + 2 * axis - side);
                }
                return cells;
            }
            foreach (var axis in kind == JumpPiece.Rook ? Axes : Diagonals) Add(board, cells, center + axis * ring);
            return cells;
        }

        private static void Add(ChessBoardGrid board, List<Vector2Int> cells, Vector2Int cell)
        {
            if (board.Contains(cell)) cells.Add(cell);
        }
    }
}
