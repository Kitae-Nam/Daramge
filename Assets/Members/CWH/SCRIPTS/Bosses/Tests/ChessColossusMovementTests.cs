using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    public sealed class ChessColossusMovementTests
    {
        private ChessBoardGrid _board;
        private static readonly Vector2Int Start = new Vector2Int(2, 2);

        [SetUp] public void SetUp() => _board = new GameObject("Board").AddComponent<ChessBoardGrid>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_board.gameObject);

        [TestCase(3, 0, ChessColossusMoveKind.Rook)]
        [TestCase(3, 3, ChessColossusMoveKind.Bishop)]
        [TestCase(2, 1, ChessColossusMoveKind.Knight)]
        public void PlansTheMatchingChessMove(int x, int y, ChessColossusMoveKind kind)
        {
            Vector2Int goal = Start + new Vector2Int(x, y);
            Assert.That(Plan(goal, (_, to) => _board.Contains(to), out var move), Is.True);
            Assert.That(move.Kind, Is.EqualTo(kind));
            Assert.That(move.Destination, Is.EqualTo(goal));
        }

        [Test]
        public void KnightCanUseTheOtherCornerWhenTheLongLegIsBlocked()
        {
            Vector2Int goal = Start + new Vector2Int(2, 1);
            Vector2Int blocked = Start + Vector2Int.right;
            Assert.That(Plan(goal, (_, to) => to != blocked, out var move), Is.True);
            Assert.That(move.Kind, Is.EqualTo(ChessColossusMoveKind.Knight));
            Assert.That(move.FirstWaypoint, Is.EqualTo(Start + Vector2Int.up));
            Assert.That(move.Destination, Is.EqualTo(goal));
        }

        [Test]
        public void SlidingMoveCannotSkipAnOccupiedIntermediateCell()
        {
            Vector2Int goal = Start + new Vector2Int(3, 0);
            Vector2Int blocked = Start + Vector2Int.right;
            Assert.That(Plan(goal, (_, to) => to != blocked, out var move), Is.True);
            Assert.That(move.Destination, Is.Not.EqualTo(goal));
            Assert.That(move.Destination, Is.Not.EqualTo(blocked));
        }

        [Test]
        public void FindsADetourEvenWhenItsFirstMoveIsFartherFromTheGoal()
        {
            Vector2Int goal = Start + Vector2Int.right;
            Vector2Int detour = Start + Vector2Int.up;
            bool Traverse(Vector2Int from, Vector2Int to) =>
                (from == Start && to == detour) || (from == detour && to == goal);
            Assert.That(Plan(goal, Traverse, out var move), Is.True);
            Assert.That(move.Destination, Is.EqualTo(detour));
        }

        [Test]
        public void StopsWhenNoLegalMoveExists()
        {
            Assert.That(Plan(Start + Vector2Int.right, (_, _) => false, out _), Is.False);
        }

        [Test]
        public void DragAdvancesOnlyAfterTheHandsPlantInEachCycle()
        {
            Assert.That(ChessColossusDragGait.LegProgress(.2f, 2, out float firstReach), Is.Zero);
            Assert.That(firstReach, Is.EqualTo(.4f).Within(.001f));
            Assert.That(ChessColossusDragGait.LegProgress(.35f, 2, out _), Is.InRange(0.01f, .49f));
            Assert.That(ChessColossusDragGait.LegProgress(.7f, 2, out _), Is.EqualTo(.5f).Within(.001f));
            Assert.That(ChessColossusDragGait.LegProgress(1f, 2, out _), Is.EqualTo(1f));
        }

        [TestCase(ChessColossusMoveKind.Rook)]
        [TestCase(ChessColossusMoveKind.Knight)]
        [TestCase(ChessColossusMoveKind.Bishop)]
        public void PrefersTheNextPieceWhenSeveralMovesReachTheGoalInOneTurn(ChessColossusMoveKind preferred)
        {
            bool IsGoal(Vector2Int cell) => cell == Start + new Vector2Int(2, 0) ||
                cell == Start + new Vector2Int(2, 1) || cell == Start + new Vector2Int(2, 2);
            Assert.That(ChessColossusMovePlanner.TryPlan(_board, Start, Start + new Vector2Int(3, 1),
                IsGoal, (_, _) => true, 3, preferred, out var move), Is.True);
            Assert.That(move.Kind, Is.EqualTo(preferred));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator PrefabAlignsToTheBoardAndFinishesWithTheAnimatorStopped()
        {
            typeof(ChessBoardGrid).GetField("_cellSize", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_board, 1.5f);
            var navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.Translate(Vector3.down * .1f), size = new Vector3(40, .2f, 40) }
            }, new Bounds(Vector3.zero, Vector3.one * 42f), Vector3.zero, Quaternion.identity);
            var navInstance = NavMesh.AddNavMeshData(navMesh);
            var target = new GameObject("Target");
            GameObject colossus = null;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/CheckmateColossusArmWalker/CheckmateColossusArmWalker.prefab");
                Assert.That(prefab, Is.Not.Null);
                var goal = new Vector2Int(6, 2);
                target.transform.position = _board.CellToWorld(goal);
                colossus = Object.Instantiate(prefab, _board.CellToWorld(Start) + new Vector3(.26f, 0, .17f), Quaternion.identity);
                Vector3 spawn = colossus.transform.position;
                var movement = colossus.GetComponent<ChessColossusGridMovement>();
                var animator = colossus.GetComponent<Animator>();
                var agent = colossus.GetComponent<NavMeshAgent>();
                movement.Bind(_board, target.transform, null);
                yield return null;
                Assert.That(agent.isOnNavMesh, Is.True);

                bool alignedAtStart = false, reached = false, playedMovement = false;
                Vector3 startCenter = _board.CellToWorld(Start);
                for (int tick = 0; tick < 180; tick++)
                {
                    reached = movement.Advance(1.55f, .05f);
                    playedMovement |= animator.GetBool("Moving");
                    Vector3 position = colossus.transform.position;
                    if (tick < 6)
                    {
                        // NavMesh can settle the height at placement. Only planar translation is part of the pull.
                        var planarOffset = new Vector2(position.x - spawn.x, position.z - spawn.z);
                        Assert.That(planarOffset.magnitude, Is.LessThan(.015f), "Body waits for both hands to plant.");
                    }
                    if (Mathf.Abs(position.x - startCenter.x) < .05f && Mathf.Abs(position.z - startCenter.z) < .05f)
                        alignedAtStart = true;
                    if (alignedAtStart) Assert.That(position.z, Is.EqualTo(startCenter.z).Within(.05f));
                    if (reached) break;
                    yield return null;
                }

                Assert.That(alignedAtStart && reached && playedMovement, Is.True);
                var end = _board.CellToWorld(goal - Vector2Int.right);
                Assert.That(colossus.transform.position.x, Is.EqualTo(end.x).Within(.05f));
                Assert.That(animator.GetBool("Moving"), Is.False);
                Assert.That(agent.updatePosition && agent.updateRotation, Is.True);
            }
            finally
            {
                if (colossus != null) Object.DestroyImmediate(colossus);
                Object.DestroyImmediate(target);
                navInstance.Remove();
                Object.DestroyImmediate(navMesh);
            }
        }
#endif

        private bool Plan(Vector2Int goal, Func<Vector2Int, Vector2Int, bool> traverse, out ChessColossusMove move) =>
            ChessColossusMovePlanner.TryPlan(_board, Start, goal, cell => cell == goal, traverse, 3, ChessColossusMoveKind.Rook, out move);
    }
}
