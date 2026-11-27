#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Presentation;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    public sealed class ChessColossusAttackTests
    {
        private const string Prefab = "Assets/Members/CWH/PREFABS/CheckmateColossusArmWalker/CheckmateColossusArmWalker.prefab";

        [Test]
        public void GridZonesUseCellCountsAndSeparateTheThreeJumpPatterns()
        {
            var board = new GameObject("Pattern board").AddComponent<ChessBoardGrid>();
            try
            {
                var start = new Vector2Int(3, 0);
                var forward = Vector2Int.up;
                Assert.That(ChessColossusAttackPattern.Rectangle(board, start, forward, 3, 1, 1).Count, Is.EqualTo(3));
                Assert.That(ChessColossusAttackPattern.Rectangle(board, start, forward, 4, 2, 1).Count, Is.EqualTo(8));
                Assert.That(ChessColossusAttackPattern.Rectangle(board, start, forward, 2, 6, 1).Count, Is.EqualTo(12));
                var landing = new Vector2Int(3, 3);
                Assert.That(ChessColossusAttackPattern.Landing(board, landing).Count, Is.EqualTo(9));
                foreach (ChessColossusAttackPattern.JumpPiece kind in System.Enum.GetValues(typeof(ChessColossusAttackPattern.JumpPiece)))
                {
                    var cells = ChessColossusAttackPattern.JumpFollowUp(board, landing, kind, 2);
                    Assert.That(cells.Count, Is.EqualTo(kind == ChessColossusAttackPattern.JumpPiece.Knight ? 8 : 4));
                    foreach (Vector2Int cell in cells)
                    {
                        Vector2Int delta = cell - landing;
                        int x = Mathf.Abs(delta.x), y = Mathf.Abs(delta.y);
                        Assert.That(kind switch
                        {
                            ChessColossusAttackPattern.JumpPiece.Rook => x + y == 2 && (x == 0 || y == 0),
                            ChessColossusAttackPattern.JumpPiece.Bishop => x == 2 && y == 2,
                            _ => x + y == 3 && Mathf.Max(x, y) == 2
                        }, Is.True);
                    }
                }
                Assert.That(ChessColossusAttackPattern.Rectangle(board, new Vector2Int(0, 0), forward, 3, 1, 1).Count, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void RealKingRegistersTheSingleColossusInItsPhaseTwoSummonPattern()
        {
            var king = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/The_King(Boss)Real.prefab");
            var pattern = king.GetComponent<SummonPattern>();
            Assert.That(pattern.enabled, Is.True);
            Assert.That(new SerializedObject(pattern).FindProperty("_minimumPhase").intValue, Is.EqualTo(2));
            var brainPatterns = new SerializedObject(king.GetComponent<PhaseOneBrain>()).FindProperty("_patterns");
            Assert.That(Enumerable.Range(0, brainPatterns.arraySize).Any(i => brainPatterns.GetArrayElementAtIndex(i).objectReferenceValue == pattern), Is.True);
            var candidates = new SerializedObject(king.GetComponent<ChessSummonCatalog>()).FindProperty("_phaseTwoPrefabs");
            var colossus = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<ChessMinion>();
            Assert.That(Enumerable.Range(0, candidates.arraySize).Any(i => candidates.GetArrayElementAtIndex(i).objectReferenceValue == colossus), Is.True);
            Assert.That(colossus.SummonCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PrefabSendsARealCameraImpulseOnTheGameplayChannel()
        {
            var managerType = System.Type.GetType("Unity.Cinemachine.CinemachineImpulseManager, Unity.Cinemachine", true);
            var manager = managerType.GetProperty("Instance").GetValue(null);
            var clear = managerType.GetMethod("Clear");
            var sample = managerType.GetMethod("GetImpulseAt");
            clear.Invoke(manager, null);
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                colossus.GetComponent<ChessColossusImpactFeedback>().PlayPrimary(
                    ChessColossusAttack.Move.Leap, Vector3.zero, Vector3.forward, 1.5f, false, Vector3.zero);
                bool received = false;
                float expiresAt = Time.time + .13f;
                do
                {
                    var arguments = new object[] { Vector3.up * 8f, false, 1, Vector3.zero, Quaternion.identity };
                    if ((bool)sample.Invoke(manager, arguments) && ((Vector3)arguments[3]).sqrMagnitude > .0001f)
                    {
                        received = true;
                        break;
                    }
                    yield return null;
                } while (Time.time < expiresAt);
                Assert.That(received, Is.True, "The prefab event must generate a measurable Cinemachine impulse.");
            }
            finally
            {
                clear.Invoke(manager, null);
                foreach (string name in new[] { "CFXR3 Hit Misc F Smoke", "CFXR2 Ground Hit" })
                {
                    var effect = GameObject.Find(name + "(Clone)");
                    if (effect != null) Object.DestroyImmediate(effect);
                }
                Object.DestroyImmediate(colossus);
            }
        }

        [Test]
        public void SlamBringsBothFistsTogetherAndHoldsAtContact()
        {
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var clip = colossus.GetComponent<Animator>().runtimeAnimatorController.animationClips
                    .Single(animation => animation.name == "ArmWalker_Slam");
                var hands = colossus.GetComponentsInChildren<Transform>()
                    .Where(bone => bone.name == "Hand_L" || bone.name == "Hand_R").ToArray();
                Assert.That(hands, Has.Length.EqualTo(2));
                float impact = ChessColossusAttack.SlamImpactTime;
                clip.SampleAnimation(colossus, 0f);
                float idleGap = Vector3.Distance(hands[0].position, hands[1].position);
                clip.SampleAnimation(colossus, impact - 6f / clip.frameRate);
                Assert.That(Vector3.Distance(hands[0].position, hands[1].position), Is.LessThan(idleGap * .60f),
                    "The hands visibly close into one wind-up pose.");
                float raised = hands.Min(hand => hand.position.y);
                clip.SampleAnimation(colossus, impact - 1f / clip.frameRate);
                float lastAirborneHeight = hands.Min(hand => hand.position.y);
                clip.SampleAnimation(colossus, impact);
                var contact = hands.Select(hand => hand.position).ToArray();
                Assert.That(raised - contact.Max(point => point.y), Is.GreaterThan(.9f));
                Assert.That(lastAirborneHeight - contact.Max(point => point.y), Is.GreaterThan(.4f),
                    "The fists travel quickly through the last frame before impact.");
                Assert.That(Vector3.Distance(contact[0], contact[1]), Is.LessThan(idleGap * .65f));
                // 클립 자체가 접촉 자세를 유지해야 회피 시에도 무게감이 남는다.
                clip.SampleAnimation(colossus, impact + 5f / clip.frameRate);
                for (int i = 0; i < hands.Length; i++)
                    Assert.That(Vector3.Distance(hands[i].position, contact[i]), Is.LessThan(.06f));
                clip.SampleAnimation(colossus, impact + 14f / clip.frameRate);
                Assert.That(hands.Where((hand, i) => Vector3.Distance(hand.position, contact[i]) > .12f).Count(),
                    Is.EqualTo(hands.Length), "Both arms release from the held pose.");
            }
            finally { Object.DestroyImmediate(colossus); }
        }

        [Test]
        public void LeapLoadsOnBothHandsAndStaysPlantedAfterLanding()
        {
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var clip = colossus.GetComponent<Animator>().runtimeAnimatorController.animationClips
                    .Single(animation => animation.name == "ArmWalker_LeapSlam");
                var bones = colossus.GetComponentsInChildren<Transform>();
                var body = bones.Single(bone => bone.name == "Body");
                var hands = bones.Where(bone => bone.name == "Hand_L" || bone.name == "Hand_R").ToArray();
                Assert.That(hands, Has.Length.EqualTo(2));
                clip.SampleAnimation(colossus, ChessColossusAttack.LeapTakeoffTime - 3f / clip.frameRate);
                Assert.That(hands.Max(hand => hand.position.y), Is.LessThan(.45f), "Both arms load against the board.");
                float loadedBody = body.position.y;
                var loadedHands = hands.Select(hand => hand.position).ToArray();
                clip.SampleAnimation(colossus, ChessColossusAttack.LeapTakeoffTime);
                Assert.That(body.position.y, Is.GreaterThan(loadedBody + .15f));
                // 몸이 먼저 펴지고 주먹은 이륙 프레임까지 제자리에 있어야 밀어내는 힘이 보인다.
                for (int i = 0; i < hands.Length; i++)
                    Assert.That(Vector3.Distance(hands[i].position, loadedHands[i]), Is.LessThan(.035f));
                clip.SampleAnimation(colossus, ChessColossusAttack.LeapApexTime);
                Assert.That(body.position.y, Is.GreaterThan(loadedBody + .1f), "The body unfolds off the ground.");
                Assert.That(Vector3.Distance(hands[0].position, hands[1].position), Is.LessThan(2.1f),
                    "The arms tuck in during flight.");
                clip.SampleAnimation(colossus, ChessColossusAttack.LeapImpactTime);
                var contact = hands.Select(hand => hand.position).ToArray();
                Assert.That(contact.Max(point => point.y), Is.LessThan(.45f));
                Assert.That(contact.Max(point => point.y) - contact.Min(point => point.y), Is.LessThan(.04f));
                // 명중하지 않아도 클립에서 양손 접점은 착지 후 잠깐 바닥에 고정된다.
                clip.SampleAnimation(colossus, ChessColossusAttack.LeapImpactTime + 6f / clip.frameRate);
                for (int i = 0; i < hands.Length; i++)
                    Assert.That(Vector3.Distance(hands[i].position, contact[i]), Is.LessThan(.04f));
            }
            finally { Object.DestroyImmediate(colossus); }
        }

        [TestCase(ChessColossusGridEffects.Piece.Rook, "Black Rook", 1f)]
        [TestCase(ChessColossusGridEffects.Piece.Bishop, "Black Bishop", 1f)]
        [TestCase(ChessColossusGridEffects.Piece.Knight, "Black Knight", 1f)]
        [TestCase(ChessColossusGridEffects.Piece.Rook, "Black Rook", 1.25f)]
        public void JumpPiecesUseTheInspectorHeightRatio(ChessColossusGridEffects.Piece kind, string pieceName, float ratio)
        {
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var effects = colossus.GetComponent<ChessColossusGridEffects>();
                if (ratio != 1f)
                {
                    var settings = new SerializedObject(effects);
                    settings.FindProperty("_jumpPieceHeightRatio").floatValue = ratio;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                const float cellSize = 1.5f;
                const float surface = .2f;
                effects.Strike(new[] { Vector3.up }, cellSize, kind, surface);
                var piece = GameObject.Find(pieceName + "(Clone)");
                Assert.That(piece, Is.Not.Null);
                Bounds bounds = piece.GetComponentInChildren<MeshRenderer>().bounds;
                Assert.That(bounds.size.y, Is.EqualTo(cellSize * ratio).Within(.01f));
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(surface), "The piece begins below the visual board surface.");
            }
            finally { Object.DestroyImmediate(colossus); }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void SlamRooksFollowTheirWarnedLanesAndStopAtTheBoardEdge(bool edge, bool wallBlocked)
        {
            var board = new GameObject("Charge board").AddComponent<ChessBoardGrid>();
            var origin = new Vector2Int(edge ? 0 : 3, edge ? 5 : 0);
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),
                board.CellToWorld(origin), Quaternion.identity);
            var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject wall = null;
            try
            {
                var health = player.AddComponent<BreakableHealth>();
                var attack = colossus.GetComponent<ChessColossusAttack>();
                if (wallBlocked)
                {
                    var settings = new SerializedObject(colossus.GetComponent<ChessColossusGridEffects>());
                    var ratio = settings.FindProperty("_chargingRookHeightRatio");
                    Assert.That(ratio, Is.Not.Null);
                    ratio.floatValue = 1.25f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                attack.Initialize(null, 2.5f, 10f, null);
                attack.BindBoard(board, null);
                Vector3 PlayerAt(int row) => board.CellToWorld(origin + Vector2Int.up * row) + Vector3.up * .8f;
                if (wallBlocked)
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.layer = LayerMask.NameToLayer("Wall");
                    wall.transform.position = PlayerAt(3) - Vector3.right * board.CellSize + Vector3.up * .6f;
                    wall.transform.localScale = new Vector3(board.CellSize * .8f, board.CellSize, .15f);
                    Physics.SyncTransforms();
                }
                player.transform.position = PlayerAt(1);
                Assert.That(attack.Prepare(player.transform), Is.True);
                attack.ShowWarning();
                var warning = GameObject.Find("Colossus follow-up cells");
                Assert.That(warning.GetComponent<MeshFilter>().sharedMesh.vertexCount / 4, Is.EqualTo(edge ? 2 : wallBlocked ? 8 : 12));
                player.transform.position = PlayerAt(edge ? 2 : 6) - Vector3.right * (wallBlocked ? board.CellSize : 0f);
                if (edge) player.transform.position += Vector3.right * (2f * board.CellSize);
                Physics.SyncTransforms();
                float before = health.CurrentHealth;
                attack.BeginAttack();
                attack.Tick(.35f);
                Transform[] Rooks() => Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                    .Select(renderer => renderer.transform.root).Distinct()
                    .Where(root => root.name == "Colossus charging rook").ToArray();
                ParticleSystem[] Dust() => Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                    .Where(particles => particles.gameObject.name == "Colossus rook charge dust").ToArray();
                ParticleSystem[] Crashes() => Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                    .Where(particles => particles.gameObject.name == "Colossus rook crash sparks").ToArray();
                var rooks = Rooks();
                Assert.That(rooks, Has.Length.EqualTo(edge ? 1 : 2), "Rooks rise during the slam windup.");
                foreach (Transform rook in rooks)
                    Assert.That(rook.GetComponentInChildren<MeshRenderer>().bounds.size.y,
                        Is.EqualTo(board.CellSize * (wallBlocked ? 1.25f : .82f)).Within(.01f));
                Assert.That(rooks.All(rook => Mathf.Abs(rook.position.z - PlayerAt(1).z) < .02f), Is.True);
                Assert.That(rooks.All(rook => Mathf.Abs(rook.position.y - board.SurfaceHeight) < .06f), Is.True);
                Assert.That(Dust(), Is.Empty, "Waiting rooks do not leave a moving trail.");
                Vector3 destination = player.transform.position;
                player.transform.position = PlayerAt(1);
                Physics.SyncTransforms();
                float frame = 1f / 60f;
                attack.Tick(ChessColossusAttack.SlamImpactTime - frame - .35f);
                Assert.That(rooks.All(rook => rook.position.z > PlayerAt(1).z), Is.True, "Rooks start before the fists land.");
                Assert.That(health.CurrentHealth, Is.EqualTo(before), "No damage during the visual lead-in.");
                player.transform.position = destination;
                Physics.SyncTransforms();
                attack.Tick(frame + .001f);
                Assert.That(rooks.All(rook =>
                    Mathf.Abs((rook.position.z - PlayerAt(1).z) / board.CellSize - 1f / 3f) < .02f), Is.True,
                    "At the slam, each rook has travelled about one third of a cell.");
                var dust = Dust();
                Assert.That(dust, Is.Not.Empty);
                Assert.That(dust.All(puff => Mathf.Abs(puff.transform.position.y - board.SurfaceHeight) < .06f &&
                    rooks.Any(rook => Mathf.Abs(rook.position.x - puff.transform.position.x) < .02f &&
                        rook.position.z > puff.transform.position.z)), Is.True, "Dust stays on the floor behind its rook.");
                Assert.That(health.CurrentHealth, Is.EqualTo(before));
                if (edge)
                {
                    attack.Cancel();
                    Assert.That(Rooks(), Is.Empty, "Cancel removes the active attack pieces.");
                    Assert.That(Dust(), Is.Empty, "Cancel clears the charge dust.");
                    Assert.That(Crashes(), Is.Empty, "Cancel is not a collision.");
                    Assert.That(warning.activeSelf, Is.False);
                    return;
                }
                attack.Tick(.32f);
                Assert.That(health.CurrentHealth, Is.EqualTo(before));
                if (wallBlocked)
                {
                    Assert.That(Rooks(), Has.Length.EqualTo(1));
                    var contact = Crashes().Single().transform.position;
                    Assert.That(contact.z, Is.EqualTo(wall.GetComponent<Collider>().bounds.min.z).Within(.06f));
                }
                else Assert.That(Crashes(), Is.Empty, "Range bursts only occur at the finish.");
                attack.Tick(.04f);
                float expectedHealth = wallBlocked ? before : before - 10f;
                Assert.That(health.CurrentHealth, Is.EqualTo(expectedHealth));
                Assert.That(Rooks(), Is.Empty);
                Assert.That(Crashes(), Has.Length.EqualTo(2));
                Assert.That(Crashes().Count(burst => Mathf.Abs(burst.transform.position.z - PlayerAt(6).z) < .06f),
                    Is.EqualTo(wallBlocked ? 1 : 2));
                Assert.That(attack.Tick(.2f), Is.True);
                Assert.That(Rooks(), Is.Empty);
                Assert.That(health.CurrentHealth, Is.EqualTo(expectedHealth));
            }
            finally
            {
                Object.DestroyImmediate(colossus);
                Object.DestroyImmediate(player);
                if (wall != null) Object.DestroyImmediate(wall);
                Object.DestroyImmediate(board.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator BossFacingYieldsToTheLockedWarningAndResumesAfterAttack()
        {
            var board = new GameObject("Facing board").AddComponent<ChessBoardGrid>();
            var origin = new Vector2Int(3, 2);
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),
                board.CellToWorld(origin), Quaternion.identity);
            var player = new GameObject("Facing target").transform;
            try
            {
                var facing = colossus.GetComponent<BossFacing>() ?? colossus.AddComponent<BossFacing>();
                colossus.GetComponent<BossTarget>().SetTarget(player);
                var attack = colossus.GetComponent<ChessColossusAttack>();
                attack.Initialize(null, 2.5f, 10f, null);
                attack.BindBoard(board, null);
                player.position = board.CellToWorld(origin + Vector2Int.up);
                Assert.That(attack.Prepare(player), Is.True);
                attack.ShowWarning();
                player.position = board.CellToWorld(origin + Vector2Int.right);
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, Vector3.forward), Is.LessThan(.01f));
                attack.BeginAttack();
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, Vector3.forward), Is.LessThan(.01f));
                Assert.That(attack.Tick(2f), Is.True);
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, Vector3.forward), Is.GreaterThan(.01f));

                attack.BindBoard(board, null);
                Assert.That(attack.Prepare(player), Is.True);
                attack.ShowWarning();
                Assert.That(facing.enabled, Is.False);
                attack.Cancel();
                Assert.That(facing.enabled, Is.True);
                facing.enabled = false;
                Assert.That(attack.Prepare(player), Is.True);
                attack.ShowWarning();
                attack.Cancel();
                Assert.That(facing.enabled, Is.False, "Cancel preserves an already disabled component.");
            }
            finally
            {
                Object.DestroyImmediate(colossus);
                Object.DestroyImmediate(player.gameObject);
                Object.DestroyImmediate(board.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator ColossusFacesOneOfEightDirectionsWhenTracking()
        {
            var board = new GameObject("Eight direction board").AddComponent<ChessBoardGrid>();
            var origin = new Vector2Int(3, 2);
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),
                board.CellToWorld(origin), Quaternion.identity);
            var player = new GameObject("Facing target").transform;
            try
            {
                var movement = colossus.GetComponent<ChessColossusGridMovement>();
                movement.Bind(board, player, null);
                var attack = colossus.GetComponent<ChessColossusAttack>();
                attack.Initialize(null, 2.5f, 10f, null);
                attack.BindBoard(board, null);
                player.position = board.CellToWorld(origin + Vector2Int.up + Vector2Int.right) + Vector3.forward * .4f;
                yield return null;
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, new Vector3(1f, 0f, 1f)), Is.LessThan(.02f));

                var facing = colossus.GetComponent<ChessColossusFacing>();
                facing.SetMoveDirection(new Vector3(-1f, 0f, -.9f));
                yield return null;
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, new Vector3(-1f, 0f, -1f)), Is.LessThan(.02f));

                Assert.That(attack.Prepare(player), Is.True);
                attack.ShowWarning();
                player.position = board.CellToWorld(origin + Vector2Int.left);
                yield return null;
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, Vector3.forward), Is.LessThan(.02f));
                attack.BeginAttack();
                yield return null;
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, Vector3.forward), Is.LessThan(.02f));
                facing.ClearMoveDirection();
                attack.Cancel();
                yield return null;
                yield return null;
                Assert.That(Vector3.Angle(colossus.transform.forward, Vector3.left), Is.LessThan(.02f));
            }
            finally
            {
                Object.DestroyImmediate(colossus);
                Object.DestroyImmediate(player.gameObject);
                Object.DestroyImmediate(board.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CyclesAllThreeAttacksAndDamagesOnceAtTheLockedWarning()
        {
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.Translate(Vector3.down * .1f), size = new Vector3(40, .2f, 40) }
            }, new Bounds(Vector3.zero, Vector3.one * 42f), Vector3.zero, Quaternion.identity);
            var nav = NavMesh.AddNavMeshData(data);
            var board = new GameObject("Board").AddComponent<ChessBoardGrid>();
            typeof(ChessBoardGrid).GetField("_cellSize", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(board, 1.5f);
            var surface = new GameObject("Visual board surface").transform;
            surface.SetParent(board.transform, false);
            surface.position = Vector3.down * .2f;
            typeof(ChessBoardGrid).GetField("_visualSurface", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(board, surface);
            Assert.That(board.CellToWorld(Vector2Int.zero).y, Is.Zero, "The visible surface does not move the gameplay grid.");
            var colossus = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),
                board.CellToWorld(new Vector2Int(3, 3)), Quaternion.identity);
            var victim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var health = victim.AddComponent<BreakableHealth>();
            var knockback = victim.AddComponent<KnockbackReceiver>();
            try
            {
                yield return null;
                var animator = colossus.GetComponent<Animator>();
                var agent = colossus.GetComponent<NavMeshAgent>();
                var attack = colossus.GetComponent<ChessColossusAttack>();
                var warning = colossus.GetComponentInChildren<AreaTelegraphPresenter>();
                var sweep = colossus.GetComponent<ChessSweepPresentation>();
                var feedback = new SerializedObject(sweep);
                var grid = new SerializedObject(colossus.GetComponent<ChessColossusGridEffects>());
                foreach (string field in new[] { "_tileMaterial", "_warningMaterial", "_rook", "_bishop", "_knight" })
                    Assert.That(grid.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                foreach (string field in new[] { "_scratchMaterial", "_scrapeSound" })
                    Assert.That(feedback.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var impact = colossus.GetComponent<ChessColossusImpactFeedback>();
                var impactSettings = new SerializedObject(impact);
                foreach (string field in new[] { "_groundEffect", "_landingRing", "_sweepSparks", "_hitEffect", "_groundSound", "_hitSound" })
                    Assert.That(impactSettings.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var impulse = (UnityEvent<float>)typeof(ChessColossusImpactFeedback)
                    .GetField("_cameraImpact", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(impact);
                Assert.That(impulse.GetPersistentTarget(0), Is.Not.Null);
                Assert.That(impulse.GetPersistentMethodName(0), Is.EqualTo("GenerateImpulseWithForce"));
                var impulses = new List<float>();
                impulse.AddListener(impulses.Add);
                feedback.FindProperty("_scratchLifetime").floatValue = .08f;
                feedback.ApplyModifiedPropertiesWithoutUndo();
                ChessSweepGroundMark mark = null;
                uint partialIndices = 0;
                Assert.That(colossus.GetComponent<ChessMinion>().SummonCount, Is.EqualTo(1));
                Assert.That(agent.isOnNavMesh, Is.True);
                attack.Initialize(agent, 2.5f, 10f, warning);
                attack.BindBoard(board, null);

                string[] states = { "Slam", "LeapSlam", "Sweep" };
                float[] impactTimes = { ChessColossusAttack.SlamImpactTime, ChessColossusAttack.LeapImpactTime, ChessColossusAttack.SweepImpactTime };
                for (int i = 0; i < states.Length; i++)
                {
                    Vector3 start = colossus.transform.position;
                    victim.transform.position = start + new Vector3(0f, .8f, i == 2 ? 1.5f : 2.45f);
                    Assert.That(attack.Prepare(victim.transform), Is.True);
                    attack.ShowWarning();
                    var mainWarning = GameObject.Find("Colossus main cells");
                    var mainTiles = mainWarning.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(mainTiles.vertexCount / 4, Is.EqualTo(i == 0 ? 8 : i == 1 ? 9 : 3));
                    var edges = new List<Vector4>();
                    mainTiles.GetUVs(1, edges);
                    Assert.That(edges[0].y, Is.Zero, "Do not outline the edge shared with the next warning tile.");
                    Assert.That(edges[4].x, Is.Zero);
                    var fillCoordinates = new List<Vector3>();
                    mainTiles.GetUVs(0, fillCoordinates);
                    var vertices = mainTiles.vertices;
                    // 정렬 순서와 관계없이 같은 월드 Z의 정점은 같은 시점에 채워져야 한다.
                    for (int v = 0; v < vertices.Length; v++)
                    {
                        float expected = Mathf.InverseLerp(mainTiles.bounds.min.z, mainTiles.bounds.max.z, vertices[v].z);
                        Assert.That(fillCoordinates[v].z, Is.EqualTo(expected).Within(.001f));
                    }
                    attack.SetWarningProgress(.65f);
                    var properties = new MaterialPropertyBlock();
                    mainWarning.GetComponent<Renderer>().GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat("_Progress"), Is.EqualTo(.65f).Within(.001f));
                    Assert.That(properties.GetFloat("_Opacity"), Is.GreaterThanOrEqualTo(.9f));
                    Vector3 marked = mainTiles.bounds.center;
                    Assert.That(marked.y, Is.EqualTo(surface.position.y + .004f).Within(.001f));
                    Vector3[] slamContacts = null;
                    if (i == 0)
                    {
                        var slam = animator.runtimeAnimatorController.animationClips.Single(clip => clip.name == "ArmWalker_Slam");
                        var hands = colossus.GetComponentsInChildren<Transform>().Where(bone => bone.name == "Hand_L" || bone.name == "Hand_R").ToArray();
                        slam.SampleAnimation(colossus, .60f);
                        float raised = hands.Min(hand => hand.position.y);
                        slam.SampleAnimation(colossus, ChessColossusAttack.SlamImpactTime);
                        Assert.That(raised - hands.Max(hand => hand.position.y), Is.GreaterThan(.65f), "Both fists drop at the slam frame.");
                        slamContacts = hands.Select(hand => hand.position).ToArray();
                    }
                    attack.BeginAttack();
                    animator.Update(0f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + states[i]), Is.True, states[i]);
                    float priorHealth = health.CurrentHealth;
                    float elapsed = 0f;
                    if (i == 1)
                    {
                        int dustBeforeLaunch = GroundEffects("CFXR3 Hit Misc F Smoke").Length;
                        float launchSample = ChessColossusAttack.LeapTakeoffTime + 3f / 60f;
                        Assert.That(attack.Tick(launchSample), Is.False);
                        Assert.That(colossus.transform.position.y - start.y, Is.GreaterThan(.6f),
                            "The root clears the board quickly after the arms push off.");
                        var launchDust = GroundEffects("CFXR3 Hit Misc F Smoke");
                        Assert.That(launchDust, Has.Length.EqualTo(dustBeforeLaunch + 2));
                        Assert.That(launchDust.Count(effect => effect.localScale.x < .65f), Is.EqualTo(2));
                        elapsed = ChessColossusAttack.LeapApexTime;
                        Assert.That(attack.Tick(elapsed - launchSample), Is.False);
                        Assert.That(GroundEffects("CFXR3 Hit Misc F Smoke"), Has.Length.EqualTo(dustBeforeLaunch + 2));
                        Vector2 travel = new Vector2(marked.x - start.x, marked.z - start.z);
                        Vector2 moved = new Vector2(colossus.transform.position.x - start.x, colossus.transform.position.z - start.z);
                        Assert.That(travel.sqrMagnitude, Is.GreaterThan(.25f));
                        // 정점까지 목표 칸에 거의 다가가고, 남은 구간에서 내려꽂는 궤적이다.
                        Assert.That(Vector2.Dot(travel, moved) / travel.sqrMagnitude, Is.InRange(.73f, .86f));
                        Assert.That(health.CurrentHealth, Is.EqualTo(priorHealth));
                    }
                    Assert.That(attack.Tick(impactTimes[i] - .03f - elapsed), Is.False);
                    Assert.That(health.CurrentHealth, Is.EqualTo(priorHealth));
                    Assert.That(impulses.Count, Is.EqualTo(i));
                    if (i == 1)
                    {
                        Assert.That(colossus.transform.position.y, Is.GreaterThan(start.y));
                        Assert.That(agent.updatePosition, Is.False);
                    }
                    if (i == 2)
                    {
                        mark = Object.FindAnyObjectByType<ChessSweepGroundMark>();
                        Assert.That(mark, Is.Not.Null);
                        partialIndices = mark.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0);
                        Assert.That(partialIndices, Is.GreaterThan(0));
                    }
                    Physics.SyncTransforms();
                    Assert.That(attack.Tick(.035f), Is.False);
                    Assert.That(health.CurrentHealth, Is.EqualTo(priorHealth - 10f));
                    Assert.That(impulses.Count, Is.EqualTo(i + 1));
                    if (i == 0)
                    {
                        var dust = GroundEffects("CFXR3 Hit Misc F Smoke");
                        Assert.That(dust, Has.Length.EqualTo(2), "Both fists kick up dust.");
                        // 먼지는 그리드 중심이 아니라 애니메이션에서 각 주먹이 닿은 바닥에 떠야 한다.
                        foreach (Vector3 contact in slamContacts)
                            Assert.That(dust.Any(effect =>
                                Vector2.Distance(new Vector2(effect.position.x, effect.position.z),
                                    new Vector2(contact.x, contact.z)) < .05f), Is.True);
                    }
                    else if (i == 1)
                    {
                        var ring = GroundEffects("CFXR2 Ground Hit").Single();
                        Assert.That(ring.position.x, Is.EqualTo(marked.x).Within(.01f));
                        Assert.That(ring.position.z, Is.EqualTo(marked.z).Within(.01f));
                    }
                    else Assert.That(GroundEffects("CFXR Hit A (Red)"), Has.Length.EqualTo(2));
                    Assert.That(animator.speed, Is.Zero);
                    Assert.That(attack.Tick(.02f), Is.False);
                    Assert.That(animator.speed, Is.Zero, "The impact pose holds briefly.");
                    if (i == 1)
                    {
                        Assert.That(colossus.transform.position.z, Is.EqualTo(marked.z).Within(.02f));
                        Assert.That(colossus.transform.position.z, Is.EqualTo(board.CellToWorld(board.WorldToCell(marked)).z).Within(.02f));
                        Assert.That(agent.isOnNavMesh && agent.updatePosition && agent.updateRotation, Is.True);
                    }
                    if (i == 2)
                    {
                        Vector3 push = knockback.KnockBackVelocity;
                        Assert.That(push.z, Is.GreaterThan(0f), "Sweep pushes the player away from the colossus.");
                    }
                    else Assert.That(knockback.IsKnockedBack, Is.False);
                    Assert.That(attack.Tick(1f), Is.True);
                    Assert.That(health.CurrentHealth, Is.EqualTo(priorHealth - 10f));
                    Assert.That(animator.speed, Is.EqualTo(1f));
                    Assert.That(impulses.Count, Is.EqualTo(i + 1));
                    if (i == 0) Assert.That(GameObject.Find("Colossus charging rook"), Is.Null);
                    if (i == 1) Assert.That(GameObject.Find("Black Rook(Clone)"), Is.Not.Null);
                    if (i == 2)
                    {
                        Assert.That(mark.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0), Is.GreaterThan(partialIndices));
                    }
                }

                Transform[] GroundEffects(string name)
                {
                    var effects = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                        .Where(effect => effect.gameObject.name == name + "(Clone)").Select(effect => effect.transform).ToArray();
                    Assert.That(effects.All(effect => Mathf.Abs(effect.position.y - surface.position.y) < .06f), Is.True,
                        "Ground effects sit on the visible board rather than the navigation surface.");
                    return effects;
                }
                Assert.That(impulses[1], Is.GreaterThan(impulses[0]).And.GreaterThan(impulses[2]));

                victim.transform.position = colossus.transform.position + new Vector3(0f, .8f, 2.45f);
                Assert.That(attack.Prepare(victim.transform), Is.True);
                attack.ShowWarning();
                victim.transform.position += Vector3.right * 6f;
                Physics.SyncTransforms();
                attack.BeginAttack();
                float beforeDodge = health.CurrentHealth;
                attack.Tick(ChessColossusAttack.SlamImpactTime + .01f);
                Assert.That(health.CurrentHealth, Is.EqualTo(beforeDodge), "The impact must not track an evading target.");
                Assert.That(animator.runtimeAnimatorController.animationClips.Count(clip => clip.name == "ArmWalker_LeapSlam" || clip.name == "ArmWalker_Sweep"), Is.EqualTo(2));

                attack.Cancel();
                typeof(ChessColossusAttack).GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attack, ChessColossusAttack.Move.Sweep);
                victim.transform.position = colossus.transform.position + new Vector3(0f, .8f, 1.5f);
                Assert.That(attack.Prepare(victim.transform), Is.True);
                attack.ShowWarning();
                victim.transform.position += Vector3.right * 6f;
                knockback.ReceiveKnockBack(Vector3.left, 1f, .3f);
                Physics.SyncTransforms();
                attack.BeginAttack();
                attack.Tick(ChessColossusAttack.SweepImpactTime + .01f);
                Assert.That(health.CurrentHealth, Is.EqualTo(beforeDodge));
                Assert.That(knockback.KnockBackVelocity, Is.EqualTo(Vector3.left));
                Assert.That(animator.speed, Is.EqualTo(1f));
                Assert.That(impulses.Count, Is.EqualTo(5), "Each floor impact plays once even when the player dodges.");
                Assert.That(impulses[3], Is.GreaterThan(0f).And.LessThan(impulses[0]), "A floor hit is softer than a confirmed slam.");
                Assert.That(impulses[4], Is.GreaterThan(0f).And.LessThan(impulses[2]), "A dodged sweep gives a lighter shake.");
                Assert.That(Object.FindObjectsByType<ChessSweepGroundMark>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
                attack.Tick(1f);

                typeof(ChessColossusAttack).GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attack, ChessColossusAttack.Move.Sweep);
                victim.transform.position = colossus.transform.position + new Vector3(0f, .8f, 1.5f);
                health.CanTakeDamage = false;
                Physics.SyncTransforms();
                Assert.That(attack.Prepare(victim.transform), Is.True);
                attack.ShowWarning();
                attack.BeginAttack();
                attack.Tick(ChessColossusAttack.SweepImpactTime + .01f);
                Assert.That(health.CurrentHealth, Is.EqualTo(beforeDodge));
                Assert.That(knockback.KnockBackVelocity, Is.EqualTo(Vector3.left));
                Assert.That(animator.speed, Is.EqualTo(1f));
                Assert.That(impulses.Count, Is.EqualTo(6));
                Assert.That(impulses[5], Is.EqualTo(impulses[4]), "An invulnerable target gives the same shake as a dodged sweep.");
                attack.Tick(1f);
                yield return new WaitForSeconds(.18f);
                Assert.That(mark == null, Is.True, "Ground marks fade out after the attack.");

                health.CanTakeDamage = true;
                foreach (var pattern in new[] { (new Vector2Int(2, 2), "Black Bishop(Clone)"),
                                               (new Vector2Int(2, 1), "Black Knight(Clone)") })
                {
                    typeof(ChessColossusAttack).GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attack, ChessColossusAttack.Move.Leap);
                    victim.transform.position = colossus.transform.position + new Vector3(0f, .8f, 1.5f);
                    Assert.That(attack.Prepare(victim.transform), Is.True);
                    attack.ShowWarning();
                    Vector3 landingCenter = GameObject.Find("Colossus main cells").GetComponent<MeshFilter>().sharedMesh.bounds.center;
                    Vector2Int outerCell = board.WorldToCell(landingCenter) + pattern.Item1;
                    Assert.That(board.Contains(outerCell), Is.True);
                    victim.transform.position = board.CellToWorld(outerCell) + Vector3.up * .8f;
                    Physics.SyncTransforms();
                    float before = health.CurrentHealth;
                    attack.BeginAttack();
                    attack.Tick(ChessColossusAttack.LeapImpactTime + .02f);
                    Assert.That(health.CurrentHealth, Is.EqualTo(before), "The outer tile is outside the landing square.");
                    attack.Tick(.16f);
                    Assert.That(GameObject.Find(pattern.Item2), Is.Not.Null, "Only the selected chess piece rises.");
                    Assert.That(health.CurrentHealth, Is.EqualTo(before), "The rise has a short lead before damage.");
                    attack.Tick(.07f);
                    Assert.That(health.CurrentHealth, Is.EqualTo(before - 10f), "The selected outer tile damages on emergence.");
                    Assert.That(attack.Tick(1f), Is.True);
                }
            }
            finally
            {
                foreach (var mark in Object.FindObjectsByType<ChessSweepGroundMark>(FindObjectsSortMode.None))
                    Object.DestroyImmediate(mark.gameObject);
                Object.DestroyImmediate(colossus);
                Object.DestroyImmediate(victim);
                Object.DestroyImmediate(board.gameObject);
                nav.Remove();
                Object.DestroyImmediate(data);
            }
        }
    }
}
#endif
