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
using UnityEngine.AI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    /// <summary>실제 프리팹으로 검 소유권, 중단 정리, 페이즈 전환과 강화 패턴의 상호작용을 검증한다.</summary>
    public sealed class ChessPhaseTwoTests
    {
        private GameObject _boss;
        private GameObject _player;
        private NavMeshData _nav;
        private NavMeshDataInstance _navInstance;
        private BossPatternRunner Runner => _boss.GetComponent<BossPatternRunner>();
        private BossSwordInventory Inventory => _boss.GetComponent<BossSwordInventory>();
        private PhaseTwoEncounter PhaseTwo => _boss.GetComponent<PhaseTwoEncounter>();
        private ChessCombatProfiles Profiles => _boss.GetComponent<ChessCombatProfiles>();

        [SetUp]
        public void Setup()
        {
            _nav = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one), size = new Vector3(80f, 0.2f, 80f) }
            }, new Bounds(Vector3.zero, Vector3.one * 100f), Vector3.zero, Quaternion.identity);
            _navInstance = NavMesh.AddNavMeshData(_nav);
#if UNITY_EDITOR
            _boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/PhaseOne/PhaseOneBoss.prefab"));
#endif
            _boss.GetComponent<PhaseOneBrain>().enabled = false;
            _boss.GetComponent<BossMovementStateController>().SetMovementAllowed(false);
            _boss.GetComponent<NavMeshAgent>().Warp(Vector3.zero);
            _player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _player.transform.position = new Vector3(0f, 1f, 5f);
            _player.AddComponent<DashTestReceiver>();
            _boss.GetComponent<BossTarget>().SetTarget(_player.transform);
            Set(Runner, "_patternInterval", 0f);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (DashScorchField field in Object.FindObjectsByType<DashScorchField>(FindObjectsSortMode.None))
                Object.DestroyImmediate(field.gameObject);
            if (_boss != null) Object.DestroyImmediate(_boss);
            if (_player != null) Object.DestroyImmediate(_player);
            if (_navInstance.valid) _navInstance.Remove();
            if (_nav != null) Object.DestroyImmediate(_nav);
        }

        private static void Set(object owner, string field, object value)
        {
            for (System.Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                if (info == null) continue;
                info.SetValue(owner, value);
                return;
            }
            Assert.Fail("Missing field: " + field);
        }

        private void FastMelee()
        {
            var melee = _boss.GetComponent<MeleeComboPattern>();
            Set(melee, "_warningDuration", 0.12f);
            Set(melee, "_secondWarning", 0.04f);
            Set(melee, "_swingDuration", 0.08f);
            Set(melee, "_finishHoldDuration", 0.02f);
            Set(melee, "_recoveryDuration", 0.01f);
            Set(melee, "_cooldown", 0f);
        }

        private sealed class SingleStrikeTuning : IMeleeTuningSource
        {
            public MeleeTuning GetMelee(float normalSwing, float normalWarning) => new MeleeTuning(1, 0.05f, 0.03f, 1f);
        }

        [UnityTest]
        public IEnumerator MeleeAcceptsAnotherTuningSourceWithoutKnowingItsImplementation()
        {
            FastMelee();
            var melee = _boss.GetComponent<MeleeComboPattern>();
            Set(melee, "_profiles", new SingleStrikeTuning());
            Assert.That(Runner.TryRun(melee), Is.True);
            yield return new WaitForSeconds(0.6f);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(4));
        }

        private IEnumerator EnterPhaseTwo()
        {
            Set(PhaseTwo, "_transitionDuration", 0.01f);
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            float deadline = Time.time + 1f;
            while (!PhaseTwo.IsReady && Time.time < deadline) yield return null;
            Assert.That(PhaseTwo.IsReady, Is.True);
            _boss.GetComponent<BossMovementStateController>().SetMovementAllowed(false);
        }

        [UnityTest]
        public IEnumerator ChargeOwnsTheLastSwordWithoutEarlyDamage()
        {
            for (int i = 1; i < Inventory.Swords.Count; i++) Inventory.Swords[i].gameObject.SetActive(false);
            FastMelee();
            Assert.That(Runner.TryRun(_boss.GetComponent<MeleeComboPattern>()), Is.True);
            yield return null; yield return null;
            Assert.That(Inventory.HasHeldSword, Is.True);
            Assert.That(Inventory.HasOrbitingSword, Is.False);
            Assert.That(_boss.GetComponent<MeleeComboPattern>().CanExecute(), Is.True);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
            Assert.That(Inventory.Swords[0].SwingTrail.IsEmitting, Is.False);
            yield return new WaitForSeconds(0.65f);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(2));
            Assert.That(Inventory.Swords[0].State, Is.EqualTo(BossSwordState.Orbiting));
        }

        [UnityTest]
        public IEnumerator GateCancellationReturnsChargedSwordAndClearsTrail()
        {
            FastMelee();
            Runner.TryRun(_boss.GetComponent<MeleeComboPattern>());
            yield return null; yield return null;
            _boss.GetComponent<BossCombatGate>().Block(this);
            Assert.That(Runner.CurrentPattern, Is.Null);
            Assert.That(Inventory.HasHeldSword, Is.False);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(4));
            foreach (BossSword sword in Inventory.Swords) Assert.That(sword.SwingTrail.IsEmitting, Is.False);
        }

        [UnityTest]
        public IEnumerator NaturalRecallReusesPhysicalSwordAndMeleeCanUseAnother()
        {
            ThrownBossSword thrown = _boss.GetComponent<BossSwordController>().Throw(_player.transform, Vector3.forward * 10f, 2f);
            BossSword physical = thrown.GetComponent<BossSword>();
            Assert.That(physical, Is.Not.Null);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(3));
            Assert.That(_boss.GetComponent<MeleeComboPattern>().CanExecute(), Is.True);
            Set(thrown, "_flightDuration", 0.02f);
            Set(thrown, "_embeddedDuration", 0.03f);
            // 자연 회수는 이제 뽑기/세우기/복귀 연출을 기다린다. 즉시 복귀를 가정하지 않는다.
            float deadline = Time.time + 3f;
            while (physical != null && physical.State != BossSwordState.Orbiting && Time.time < deadline)
                yield return null;
            Assert.That(physical != null, Is.True);
            Assert.That(physical.State, Is.EqualTo(BossSwordState.Orbiting));
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(4));
            Assert.That(_boss.GetComponent<BossHealth>().CurrentHealth, Is.EqualTo(1000f));
        }

        [UnityTest]
        public IEnumerator BreakingPhysicalSwordRemovesItAndRewardsOnlyOnce()
        {
            ThrownBossSword thrown = _boss.GetComponent<BossSwordController>().Throw(_player.transform, Vector3.forward * 10f, 2f);
            Set(thrown, "_flightDuration", 0.02f);
            yield return new WaitForSeconds(0.08f);
            BreakableHealth health = thrown.GetComponent<BreakableHealth>();
            health.ReceiveDamage(1000f);
            health.ReceiveDamage(1000f);
            yield return null;
            Assert.That(thrown == null, Is.True);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(3));
            Assert.That(_boss.GetComponent<BossHealth>().CurrentHealth, Is.EqualTo(900f));
        }

        [UnityTest]
        public IEnumerator TransitionBlocksCombatThenResumesAsBlackKing()
        {
            var appearance = _boss.GetComponent<ChessPhaseAppearance>();
            var bodyMesh = (MeshFilter)typeof(ChessPhaseAppearance).GetField("_bodyMesh", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(appearance);
            Mesh before = bodyMesh.sharedMesh;
            Set(PhaseTwo, "_transitionDuration", 0.05f);
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            Assert.That(_boss.GetComponent<BossPhaseController>().CurrentPhase, Is.EqualTo(2));
            Assert.That(_boss.GetComponent<BossCombatGate>().IsOpen, Is.False);
            Assert.That(Runner.TryRun(_boss.GetComponent<MeleeComboPattern>()), Is.False);
            yield return new WaitForSeconds(0.12f);
            Assert.That(PhaseTwo.IsReady, Is.True);
            Assert.That(_boss.GetComponent<BossCombatGate>().IsOpen, Is.True);
            Assert.That(bodyMesh.sharedMesh, Is.Not.EqualTo(before));
            _boss.GetComponent<ChessKingGridMovement>().enabled = false;
            Assert.That(_boss.GetComponent<PhaseOneBrain>().TrySelectPattern(out _), Is.True);
        }

        [UnityTest]
        public IEnumerator ExternalCutsceneGateSurvivesPhaseTransition()
        {
            var gate = _boss.GetComponent<BossCombatGate>();
            gate.Block(this);
            yield return EnterPhaseTwo();
            Assert.That(gate.IsOpen, Is.False);
            gate.Unblock(this);
            Assert.That(gate.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator PhaseTwoMeleeHitsThreeTimes()
        {
            yield return EnterPhaseTwo();
            FastMelee();
            Profiles.PhaseTwo.SwingDuration = 0.05f;
            Profiles.PhaseTwo.ComboWarning = 0.03f;
            Assert.That(Runner.TryRun(_boss.GetComponent<MeleeComboPattern>()), Is.True);
            yield return new WaitForSeconds(0.8f);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(3));
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator VolleyUsesThreeActualSwordsAndGateRecallsAll()
        {
            yield return EnterPhaseTwo();
            ThrownBossSword[] swords = _boss.GetComponent<BossSwordController>().ThrowVolley(_player.transform,
                new[] { Vector3.forward * 10f, Vector3.forward * 12f, Vector3.forward * 14f }, 2f);
            Assert.That(swords.Length, Is.EqualTo(3));
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(1));
            Assert.That(_boss.GetComponent<MeleeComboPattern>().CanExecute(), Is.True);
            _boss.GetComponent<BossCombatGate>().Block(this);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(4));
            Assert.That(_boss.GetComponent<BossSwordController>().HasSword, Is.False);
        }

        [Test]
        public void WallDamageRequiresFacingWallAndAppliesOnce()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                RoarWallImpact impact = _player.AddComponent<RoarWallImpact>();
                impact.Arm(_boss.transform, Vector3.forward, 1f, 35f, ~0);
                Assert.That(impact.TryApplyWallImpact(wall.GetComponent<Collider>(), Vector3.up), Is.False);
                Assert.That(impact.TryApplyWallImpact(wall.GetComponent<Collider>(), Vector3.back), Is.True);
                Assert.That(impact.TryApplyWallImpact(wall.GetComponent<Collider>(), Vector3.back), Is.False);
                Assert.That(_player.GetComponent<DashTestReceiver>().TotalDamage, Is.EqualTo(35f));
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [Test]
        public void ExtraMinionPrefabsHaveDistinctKinds()
        {
            var catalog = _boss.GetComponent<ChessSummonCatalog>();
            var prefabs = (ChessMinion[])typeof(ChessSummonCatalog).GetField("_phaseTwoPrefabs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            Assert.That(System.Array.Exists(prefabs, minion => minion.Kind == ChessMinionKind.Rook), Is.True);
            Assert.That(System.Array.Exists(prefabs, minion => minion.Kind == ChessMinionKind.Bishop), Is.True);
        }

        [UnityTest]
        public IEnumerator RigidbodyWallContactAppliesRoarDamageOnce()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.position = new Vector3(0f, 1f, 7f);
                wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
                Rigidbody body = _player.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                _player.AddComponent<RoarWallImpact>().Arm(_boss.transform, Vector3.forward, 1f, 35f, ~0);
                Physics.SyncTransforms();
                body.linearVelocity = Vector3.forward * 10f;
                yield return new WaitForSeconds(0.4f);
                Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
                Assert.That(_player.GetComponent<DashTestReceiver>().TotalDamage, Is.EqualTo(35f));
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [UnityTest]
        public IEnumerator DisablingOwnerDefersFlyingSwordReturnUntilReactivation()
        {
            var thrown = _boss.GetComponent<BossSwordController>().Throw(_player.transform, Vector3.forward * 10f, 2f);
            BossSword sword = thrown.GetComponent<BossSword>();
            _boss.SetActive(false);
            yield return null;
            Assert.That(sword != null, Is.True);
            _boss.SetActive(true);
            yield return null; yield return null;
            Assert.That(sword.State, Is.EqualTo(BossSwordState.Orbiting));
            Assert.That(sword.transform.IsChildOf(_boss.transform), Is.True);
        }

        [UnityTest]
        public IEnumerator DestroyingOwnerDuringChargeCleansUpWithoutParentingErrors()
        {
            FastMelee();
            Runner.TryRun(_boss.GetComponent<MeleeComboPattern>());
            yield return null; yield return null;
            Assert.That(Inventory.HasHeldSword, Is.True);
            Object.Destroy(_boss);
            yield return null; yield return null;
            _boss = null;
        }

        [UnityTest]
        public IEnumerator ScorchDamagesOncePerTickAndDiesWhenGateCloses()
        {
            var settings = new ScorchSettings(5f, 0.06f, 10f);
            DashScorchField field = _boss.GetComponent<DashScorchFactory>().Spawn(_player.transform, 4f, settings);
            field.ExtendTo(Vector3.forward * 10f);
            _player.AddComponent<SphereCollider>();
            float deadline = Time.time + 1f;
            while (_player.GetComponent<DashTestReceiver>().DamageCalls == 0 && Time.time < deadline) yield return null;
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
            _boss.GetComponent<BossCombatGate>().Block(this);
            yield return null; yield return null;
            Assert.That(field == null, Is.True);
        }
    }
}
