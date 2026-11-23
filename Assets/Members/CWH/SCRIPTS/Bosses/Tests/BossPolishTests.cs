using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    /// <summary>실제 NavMesh/물리 판정과 UI 프리팹으로 벽 여유, 정렬 복구, UI 수명을 검증한다.</summary>
    public sealed class BossPolishTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private NavMeshData _nav;
        private NavMeshDataInstance _instance;
        private GameObject Track(GameObject value) { _objects.Add(value); return value; }
        private static void Set(object owner, string name, object value) => owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        [SetUp]
        public void Setup()
        {
            _nav = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                    size = new Vector3(80f, 0.2f, 80f) }
            }, new Bounds(Vector3.zero, Vector3.one * 100f), Vector3.zero, Quaternion.identity);
            _instance = NavMesh.AddNavMeshData(_nav);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject value in _objects) if (value != null) Object.DestroyImmediate(value);
            _objects.Clear();
            if (_instance.valid) _instance.Remove();
            if (_nav != null) Object.DestroyImmediate(_nav);
        }

        [TestCase(0f)]
        [TestCase(45f)]
        public void DashUsesAgentRadiusForFrontAndDiagonalWalls(float angle)
        {
            GameObject boss = Track(new GameObject("Dash clearance"));
            var agent = boss.AddComponent<NavMeshAgent>();
            agent.radius = 1.6f;
            agent.height = 2f;
            agent.Warp(Vector3.zero);
            var motor = boss.AddComponent<BossDashMotor>();
            Set(motor, "_bodyHalfDepth", 0.1f);
            Set(motor, "_wallPadding", 0.1f);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = direction * 10f + Vector3.up;
            wall.transform.rotation = Quaternion.LookRotation(direction);
            wall.transform.localScale = new Vector3(10f, 2f, 1f);
            Physics.SyncTransforms();
            Assert.That(motor.TryPlan(direction * 20f, 20f, 0.4f, out DashPath path), Is.True);
            Assert.That(path.Distance, Is.LessThanOrEqualTo(7.85f), "Agent 반경과 벽 여유를 남겨야 한다.");
            Assert.That(motor.Begin(path), Is.True);
            motor.Step(direction, path.Distance, 0.4f);
            motor.Stop();
            Assert.That(Vector3.Dot(boss.transform.position, direction), Is.LessThanOrEqualTo(7.85f));
            Assert.That(agent.updatePosition, Is.True);
        }

        [UnityTest]
        public IEnumerator GridRecoversToNextReachableCenterWhenNearestIsBlocked()
        {
            GameObject boss = Track(new GameObject("Grid recovery"));
            var agent = boss.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.Warp(new Vector3(1.6f, 0f, 0f));
            var grid = boss.AddComponent<ChessBoardGrid>();
            Set(grid, "_center", Vector3.zero);
            Set(grid, "_cellSize", 4f);
            Set(grid, "_columns", 3);
            Set(grid, "_rows", 3);
            var movement = boss.AddComponent<ChessKingGridMovement>();
            Set(movement, "_stepDuration", 0.1f);
            Set(movement, "_stopDistance", 20f);
            GameObject target = Track(new GameObject("Target"));
            target.transform.position = Vector3.right * 10f;
            boss.GetComponent<Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement.BossTarget>().SetTarget(target.transform);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = Vector3.up;
            wall.transform.localScale = new Vector3(0.8f, 2f, 0.8f);
            Physics.SyncTransforms();
            float deadline = Time.time + 1f;
            while (!movement.IsCentered && Time.time < deadline) yield return null;
            Assert.That(movement.IsCentered, Is.True);
            Assert.That(boss.transform.position.x, Is.EqualTo(4f).Within(0.06f));
        }

        [UnityTest]
        public IEnumerator SwordUiKeepsWorldSizeAndDisappearsWithItsTarget()
        {
#if UNITY_EDITOR
            GameObject cameraObject = Track(new GameObject("UI test camera"));
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            GameObject sword = Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/Sword.prefab")));
            var billboardType = System.Type.GetType("Daramge.BossUI.Members.CWH.SCRIPTS.UI.WorldBillboard, Daramge.BossUI");
            Assert.That(billboardType, Is.Not.Null);
            var billboard = Object.FindObjectsByType(billboardType, FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Cast<Component>().Single(component => (Transform)billboardType.GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component) == sword.transform);
            GameObject ui = Track(billboard.gameObject);
            Canvas canvas = ui.GetComponent<Canvas>();
            Assert.That(ui.transform.parent, Is.Null);
            Assert.That(canvas.enabled, Is.False);
            Vector3 uiScale = ui.transform.lossyScale;
            var health = sword.GetComponent<BreakableHealth>();
            health.CanTakeDamage = true;
            health.ReceiveDamage(health.MaxHealth * 0.5f);
            yield return null;
            Assert.That(canvas.enabled, Is.True);
            sword.transform.localScale *= 3f;
            sword.transform.rotation = Quaternion.Euler(45f, 70f, 0f);
            yield return null;
            Assert.That(Vector3.Distance(ui.transform.lossyScale, uiScale), Is.LessThan(0.00001f));
            health.CanTakeDamage = false;
            yield return null;
            Assert.That(canvas.enabled, Is.False);
            health.RestoreFull();
            health.CanTakeDamage = true;
            yield return null;
            Assert.That(canvas.enabled, Is.False);
            Object.Destroy(sword);
            yield return null; yield return null;
            Assert.That(ui == null, Is.True);
#else
            yield break;
#endif
        }
    }
}
