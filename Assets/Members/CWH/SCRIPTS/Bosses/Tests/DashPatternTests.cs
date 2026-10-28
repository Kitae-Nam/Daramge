using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    public sealed class DashTestReceiver : MonoBehaviour, IDamageReceiver, IKnockbackReceiver
    {
        public int DamageCalls;
        public int KnockbackCalls;
        public float TotalDamage;
        public float ReceiveDamage(float damage) { DamageCalls++; TotalDamage += damage; return damage; }
        public void ReceiveKnockBack(Vector3 direction, float speed, float duration) { KnockbackCalls++; }
    }

    /// <summary>실제 NavMesh와 Collider를 사용해 고속 명중, 고정 경로, 중단 정리를 검증한다.</summary>
    public sealed class DashPatternTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private NavMeshDataInstance _navMeshInstance;
        private NavMeshData _navMeshData;

        private GameObject Track(GameObject value) { _objects.Add(value); return value; }

        [TearDown]
        public void TearDown()
        {
            // 분리된 경고 평면도 별도로 추적하므로 테스트 실패 시에도 남지 않는다.
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            if (_navMeshInstance.valid) _navMeshInstance.Remove();
            if (_navMeshData != null) Object.DestroyImmediate(_navMeshData);
        }

        [Test]
        public void SweptHitboxHitsCrossedTargetOnlyOnceAndStopsAfterStop()
        {
            var boss = Track(new GameObject("Hitbox test"));
            var hitbox = boss.AddComponent<DashHitbox>();
            var target = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            target.transform.position = new Vector3(0f, 1f, 5f);
            var receiver = target.AddComponent<DashTestReceiver>();
            var child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            child.transform.SetParent(target.transform, false);
            var outside = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            outside.transform.position = new Vector3(3f, 1f, 5f);
            var missed = outside.AddComponent<DashTestReceiver>();
            Physics.SyncTransforms();

            hitbox.Begin();
            hitbox.Sweep(Vector3.zero, Vector3.forward * 10f, Vector3.forward, 2f, 2f, 25f, 10f, 0.3f);
            hitbox.Sweep(Vector3.zero, Vector3.forward * 10f, Vector3.forward, 2f, 2f, 25f, 10f, 0.3f);
            Assert.That(receiver.DamageCalls, Is.EqualTo(1));
            Assert.That(receiver.KnockbackCalls, Is.EqualTo(1));
            Assert.That(missed.DamageCalls, Is.Zero);
            hitbox.Stop();
            hitbox.Sweep(Vector3.zero, Vector3.forward * 10f, Vector3.forward, 2f, 2f, 25f, 10f, 0.3f);
            Assert.That(receiver.TotalDamage, Is.EqualTo(25f));
        }

        [TestCase(PrimitiveType.Plane)]
        [TestCase(PrimitiveType.Quad)]
        public void TelegraphFitsWorldDimensionsAndRestoresParent(PrimitiveType type)
        {
            GameObject plane = CreateTelegraph(type, out DashTelegraphPresenter telegraph);
            var parent = Track(new GameObject("Scaled boss"));
            parent.transform.localScale = Vector3.one * 1.2f;
            plane.transform.SetParent(parent.transform, false);
            Vector3 localScale = plane.transform.localScale;
            DashPath path = new DashPath(new Vector3(2f, 0f, 3f), Vector3.right, 12f);
            Assert.That(telegraph.Show(path, 4f, 0.7f), Is.True);
            Assert.That(plane.transform.parent, Is.Null);

            Mesh mesh = plane.GetComponent<MeshFilter>().sharedMesh;
            Vector3 uv00 = VertexAtUv(plane.transform, mesh, Vector2.zero);
            Vector3 uv10 = VertexAtUv(plane.transform, mesh, Vector2.right);
            Vector3 uv01 = VertexAtUv(plane.transform, mesh, Vector2.up);
            Assert.That(Vector3.Distance(uv00, uv10) * 0.8f, Is.EqualTo(4f).Within(0.001f));
            Assert.That(Vector3.Distance(uv00, uv01) * 0.8f, Is.EqualTo(12f).Within(0.001f));
            Assert.That(Vector3.Dot((uv01 - uv00).normalized, path.Direction), Is.GreaterThan(0.999f));
            Vector3 fixedPosition = plane.transform.position;
            parent.transform.position = Vector3.one * 20f;
            Assert.That(Vector3.Distance(plane.transform.position, fixedPosition), Is.LessThan(0.001f));
            telegraph.Hide();
            Assert.That(plane.transform.parent, Is.EqualTo(parent.transform));
            Assert.That(plane.transform.localScale, Is.EqualTo(localScale));
        }

        [Test]
        public void PlannerClipsAtPhysicalWallAndRejectsZeroDirection()
        {
            GameObject boss = CreateNavigatingBoss();
            var motor = boss.AddComponent<BossDashMotor>();
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1f, 6f);
            wall.transform.localScale = new Vector3(6f, 2f, 1f);
            Physics.SyncTransforms();
            Assert.That(motor.TryPlan(Vector3.forward * 10f, 12f, 2f, out DashPath path), Is.True);
            Assert.That(path.Distance, Is.InRange(5f, 5.5f));
            Assert.That(motor.TryPlan(boss.transform.position, 12f, 2f, out _), Is.False);
        }

        [Test]
        public void PlannerClipsAtNavMeshBoundaryWithoutColliders()
        {
            GameObject boss = CreateNavigatingBoss();
            var motor = boss.AddComponent<BossDashMotor>();
            Assert.That(motor.TryPlan(Vector3.forward, 100f, 2f, out DashPath path), Is.True);
            Assert.That(path.Distance, Is.GreaterThan(5f).And.LessThan(21f));
        }

        [UnityTest]
        public IEnumerator TargetMovingDuringWarningDoesNotChangeDashDirection()
        {
            DashPattern pattern = CreatePattern(out GameObject boss, out GameObject target);
            var runner = boss.GetComponent<BossPatternRunner>();
            Assert.That(runner.TryRun(pattern), Is.True);
            yield return WaitForState(runner, BossActionState.Warning);
            DashPath original = pattern.PlannedPath;
            target.transform.position = new Vector3(10f, 1f, 6f);
            Physics.SyncTransforms();
            yield return WaitForState(runner, BossActionState.Idle);
            Vector3 difference = boss.transform.position - original.End;
            difference.y = 0f;
            Assert.That(difference.magnitude, Is.LessThan(0.3f));
            Assert.That(pattern.PlannedPath.Direction, Is.EqualTo(original.Direction));
            Assert.That(target.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GroggyDuringDashRestoresAgentAndDisablesHitbox()
        {
            DashPattern pattern = CreatePattern(out GameObject boss, out _);
            var runner = boss.GetComponent<BossPatternRunner>();
            runner.TryRun(pattern);
            yield return WaitForState(runner, BossActionState.Executing);
            boss.GetComponent<BossGroggyController>().TryEnterGroggy(2f);
            Vector3 stopped = boss.transform.position;
            yield return null;
            yield return null;
            Assert.That(runner.State, Is.EqualTo(BossActionState.Groggy));
            Assert.That(boss.GetComponent<BossDashMotor>().IsDashing, Is.False);
            Assert.That(boss.GetComponent<DashHitbox>().IsActive, Is.False);
            Assert.That(boss.GetComponent<NavMeshAgent>().updatePosition, Is.True);
            Assert.That(Vector3.Distance(boss.transform.position, stopped), Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator CompletedDashDamagesTargetOnceAndStopsHitboxBeforeRecovery()
        {
            DashPattern pattern = CreatePattern(out GameObject boss, out GameObject target);
            var runner = boss.GetComponent<BossPatternRunner>();
            runner.TryRun(pattern);
            yield return WaitForState(runner, BossActionState.Recovery);
            Assert.That(target.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
            Assert.That(boss.GetComponent<DashHitbox>().IsActive, Is.False);
            Assert.That(boss.GetComponent<BossDashMotor>().IsDashing, Is.False);
        }

        private GameObject CreateNavigatingBoss()
        {
            _navMeshData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Box, size = new Vector3(40f, 0.2f, 40f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, 0f), Quaternion.identity, Vector3.one)
                } }, new Bounds(Vector3.zero, new Vector3(50f, 20f, 50f)), Vector3.zero, Quaternion.identity);
            Assert.That(_navMeshData, Is.Not.Null);
            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
            var boss = Track(new GameObject("Dash boss"));
            var agent = boss.AddComponent<NavMeshAgent>();
            Assert.That(agent.Warp(Vector3.zero), Is.True);
            return boss;
        }

        private DashPattern CreatePattern(out GameObject boss, out GameObject target)
        {
            boss = CreateNavigatingBoss();
            target = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            target.transform.position = new Vector3(0f, 1f, 6f);
            target.AddComponent<DashTestReceiver>();
            DashPattern pattern = boss.AddComponent<DashPattern>();
            boss.GetComponent<BossTarget>().SetTarget(target.transform);
            boss.GetComponent<BossMovement>().SetChaseEnabled(false);
            CreateTelegraph(PrimitiveType.Plane, out DashTelegraphPresenter telegraph);
            telegraph.transform.SetParent(boss.transform, false);
            SetField(typeof(DashPattern), pattern, "_telegraph", telegraph);
            SetField(typeof(DashPattern), pattern, "_dashDistance", 12f);
            SetField(typeof(DashPattern), pattern, "_dashWidth", 2f);
            SetField(typeof(BossPattern), pattern, "_warningDuration", 0.15f);
            SetField(typeof(BossPattern), pattern, "_recoveryDuration", 0.2f);
            Physics.SyncTransforms();
            return pattern;
        }

        private GameObject CreateTelegraph(PrimitiveType type, out DashTelegraphPresenter telegraph)
        {
            GameObject plane = Track(GameObject.CreatePrimitive(type));
            Object.DestroyImmediate(plane.GetComponent<Collider>());
            var presenter = plane.AddComponent<BossTelegraphPresenter>();
            SetField(typeof(BossTelegraphPresenter), presenter, "_targetRenderer", plane.GetComponent<Renderer>());
            telegraph = plane.AddComponent<DashTelegraphPresenter>();
            return plane;
        }

        private static Vector3 VertexAtUv(Transform transform, Mesh mesh, Vector2 desired)
        {
            Vector2[] uv = mesh.uv;
            for (int i = 0; i < uv.Length; i++)
                if ((uv[i] - desired).sqrMagnitude < 0.00001f) return transform.TransformPoint(mesh.vertices[i]);
            Assert.Fail("테스트 메시에서 UV 모서리를 찾지 못했습니다.");
            return Vector3.zero;
        }

        private static IEnumerator WaitForState(BossPatternRunner runner, BossActionState state)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            do
            {
                yield return null;
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"{state} 상태 전환 시간 초과: {runner.State}");
            } while (runner.State != state);
        }

        private static void SetField(System.Type type, object target, string name, object value)
            => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
