using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    public sealed class BossDamageKnockbackProbe : MonoBehaviour, IKnockbackReceiver
    {
        public int Calls { get; private set; }
        public void ReceiveKnockBack(Vector3 direction, float speed, float duration) => Calls++;
    }

    /// <summary>
    /// 실제 CHG 체력 모듈까지 피해가 전달되는 통합 검증.
    /// 기본 어셈블리는 asmdef에서 직접 참조할 수 없어 조립 경계만 리플렉션으로 접근한다.
    /// </summary>
    public sealed class BossPlayerDamageTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private GameObject _boss;
        private GameObject _player;
        private Component _health;
        private Component _binding;
        private IDamageReceiver _receiver;

        private GameObject Track(GameObject value) { _objects.Add(value); return value; }
        private static Type RequireType(string name)
        {
            Type type = Type.GetType(name + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, name);
            return type;
        }

        private static Component AddPlayerHealth(GameObject root)
        {
            var child = new GameObject("PlayerHealth Module");
            child.transform.SetParent(root.transform, false);
            Component health = child.AddComponent(RequireType("CHG._02_Scripts.Player.PlayerHealthModule"));
            health.GetType().GetMethod("Initialize").Invoke(health, new object[] { null });
            return health;
        }

        private int CurrentHealth => (int)_health.GetType().GetProperty("CurrentHealth").GetValue(_health);

        [SetUp]
        public void Setup()
        {
            _boss = Track(new GameObject("Boss"));
            _boss.AddComponent<BossHealth>();
            _boss.AddComponent<BossPhaseController>();
            _boss.AddComponent<BossTarget>();
            _player = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            _player.transform.position = new Vector3(0f, 1f, 2f);
            _health = AddPlayerHealth(_player);
            var extraCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            extraCollider.transform.SetParent(_player.transform, false);
            _boss.GetComponent<BossTarget>().SetTarget(_player.transform);
            _binding = _boss.AddComponent(RequireType("CWH.BossIntegration.BossPlayerHealthBinding"));
            Assert.That(_binding.GetType().GetMethod("TryBindTarget").Invoke(_binding, null), Is.EqualTo(true));
            _receiver = _player.GetComponent<IDamageReceiver>();
            Assert.That(_receiver, Is.Not.Null);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void SectorUsesOneThenTwoHeartsWithoutDuplicateColliderDamage()
        {
            Assert.That(CurrentHealth, Is.EqualTo(6));
            HitSector();
            Assert.That(CurrentHealth, Is.EqualTo(5));
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            Assert.That(_boss.GetComponent<BossPhaseController>().CurrentPhase, Is.EqualTo(2));
            HitSector();
            Assert.That(CurrentHealth, Is.EqualTo(3));
        }

        private void HitSector()
        {
            int count = BossAttackQuery.ApplySector(_boss.transform, Vector3.zero, Vector3.forward,
                4f, 180f, 4f, BossAttackDamage.Resolve(_boss.transform, 999f), ~0, targetRoot: _player.transform);
            Assert.That(count, Is.EqualTo(1));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDamageDoesNotBecomeMinimumOneHeart(float damage)
        {
            Assert.That(_receiver.ReceiveDamage(damage), Is.Zero);
            Assert.That(CurrentHealth, Is.EqualTo(6));
        }

        [Test]
        public void AdapterReturnsActualLossAndDoesNotDamageDeadPlayer()
        {
            Assert.That(_receiver.ReceiveDamage(10f), Is.EqualTo(6f));
            Assert.That(CurrentHealth, Is.Zero);
            Assert.That(_receiver.ReceiveDamage(1f), Is.Zero);
        }

        [Test]
        public void RebindingDoesNotAddDuplicateReceivers()
        {
            _binding.GetType().GetMethod("TryBindTarget").Invoke(_binding, null);
            Assert.That(_player.GetComponents(RequireType("CWH.BossIntegration.PlayerHealthDamageReceiver")).Length,
                Is.EqualTo(1));
        }

        [Test]
        public void DashUsesPhaseDamageOnceForSeveralColliders()
        {
            var hitbox = _boss.AddComponent<DashHitbox>();
            hitbox.TargetRoot = _player.transform;
            hitbox.Begin();
            hitbox.Sweep(Vector3.zero, Vector3.forward * 4f, Vector3.forward, 3f, 4f,
                BossAttackDamage.Resolve(_boss.transform, 999f), 0f, 0f);
            hitbox.Sweep(Vector3.zero, Vector3.forward * 4f, Vector3.forward, 3f, 4f,
                BossAttackDamage.Resolve(_boss.transform, 999f), 0f, 0f);
            Assert.That(CurrentHealth, Is.EqualTo(5));
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            hitbox.Begin();
            hitbox.Sweep(Vector3.zero, Vector3.forward * 4f, Vector3.forward, 3f, 4f,
                BossAttackDamage.Resolve(_boss.transform, 999f), 0f, 0f);
            Assert.That(CurrentHealth, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ThrownSwordSnapshotsPhaseTwoDamageAtLaunch()
        {
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            var sword = Track(new GameObject("Thrown sword"));
            var thrown = sword.AddComponent<ThrownBossSword>();
            thrown.Launch(_boss.transform, _player.transform, Vector3.up * 3f, Vector3.forward * 2f, 3f);
            yield return new WaitForSeconds(0.25f);
            Assert.That(CurrentHealth, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator PhaseTwoScorchDealsOneHeartPerTick()
        {
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            var root = Track(new GameObject("Scorch"));
            var field = root.AddComponent<DashScorchField>();
            field.Initialize(_boss.transform, _player.transform, 3f, new ScorchSettings(2f, 0.05f, 999f), null);
            field.ExtendTo(Vector3.forward * 4f);
            float deadline = Time.time + 1f;
            while (CurrentHealth == 6 && Time.time < deadline) yield return null;
            field.Cancel();
            Assert.That(CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void IndependentQueriesKeepTheirExplicitDamage()
        {
            Assert.That(BossAttackDamage.Resolve(null, 25f), Is.EqualTo(25f));
            Assert.That(BossAttackDamage.ResolveHazard(null, 10f), Is.EqualTo(10f));
        }

        [UnityTest]
        public IEnumerator PhaseOneRoarStillOnlyPushesPlayer()
        {
            var probe = _player.AddComponent<BossDamageKnockbackProbe>();
            var roar = _boss.AddComponent<RoarPattern>();
            yield return roar.Execute();
            Assert.That(probe.Calls, Is.EqualTo(1));
            Assert.That(CurrentHealth, Is.EqualTo(6));
            Assert.That(_player.GetComponent<RoarWallImpact>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator PhaseTwoRoarOnlyDealsTwoHeartsOnWallCollision()
        {
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            _boss.AddComponent<ChessCombatProfiles>();
            var probe = _player.AddComponent<BossDamageKnockbackProbe>();
            var roar = _boss.AddComponent<RoarPattern>();
            yield return roar.Execute();
            Assert.That(probe.Calls, Is.EqualTo(1));
            Assert.That(CurrentHealth, Is.EqualTo(6));
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = Vector3.right * 10f;
            var impact = _player.GetComponent<RoarWallImpact>();
            Assert.That(impact, Is.Not.Null);
            Assert.That(impact.TryApplyWallImpact(wall.GetComponent<Collider>(), Vector3.back), Is.True);
            Assert.That(CurrentHealth, Is.EqualTo(4));
            Assert.That(impact.TryApplyWallImpact(wall.GetComponent<Collider>(), Vector3.back), Is.False);
        }
    }
}
