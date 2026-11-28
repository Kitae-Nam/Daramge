using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Dash;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Summons;
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
        public IEnumerator GroggyRecallsEmbeddedSwordsWithTheUsualAnimation()
        {
            yield return EnterPhaseTwo();
            var controller = _boss.GetComponent<BossSwordController>();
            var groggy = _boss.GetComponent<BossGroggyController>();
            var bossHealth = _boss.GetComponent<BossHealth>();
            float healthBeforeRecall = bossHealth.CurrentHealth;
            int orbitingBeforeThrow = Inventory.OrbitingCount;
            ThrownBossSword[] thrown = controller.ThrowVolley(_player.transform,
                new[] { Vector3.forward * 10f, Vector3.right * 10f }, 2f);
            Assert.That(thrown.Length, Is.EqualTo(2));

            float landingDeadline = Time.time + 2f;
            while (thrown.Any(sword => sword.IsFlying) && Time.time < landingDeadline)
                yield return null;
            Assert.That(thrown.All(sword => sword.IsEmbedded), Is.True);

            Assert.That(groggy.TryEnterGroggy(), Is.True);
            // 즉시 순간이동하지 않고 복귀 중에도 실물 검을 추적하며 피격을 막는다.
            Assert.That(controller.HasSword, Is.True);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(orbitingBeforeThrow - 2));
            foreach (ThrownBossSword sword in thrown)
            {
                Assert.That(sword.IsRecalling, Is.True);
                Assert.That(sword.GetComponent<BreakableHealth>().ReceiveDamage(1000f), Is.Zero);
            }

            float recallDeadline = Time.time + 3f;
            while (controller.HasSword && Time.time < recallDeadline) yield return null;
            Assert.That(controller.HasSword, Is.False);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(orbitingBeforeThrow));
            Assert.That(thrown.All(sword => sword.GetComponent<BossSword>().State == BossSwordState.Orbiting), Is.True);
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(healthBeforeRecall));
        }

        [UnityTest]
        public IEnumerator BreakingPhysicalSwordRemovesItAndRewardsOnlyOnce()
        {
            ThrownBossSword thrown = _boss.GetComponent<BossSwordController>().Throw(_player.transform, Vector3.forward * 10f, 2f);
            Set(thrown, "_flightDuration", 0.02f);
            // 비행은 상승/공중 이동/낙하를 포함하므로 실제 착지 상태를 기다린다.
            float landingDeadline = Time.time + 2f;
            while (thrown.IsFlying && Time.time < landingDeadline) yield return null;
            Assert.That(thrown.IsEmbedded, Is.True);
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
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator VolleyUsesThreeActualSwordsAndGateRecallsAll()
        {
            yield return EnterPhaseTwo();
            ThrownBossSword[] swords = _boss.GetComponent<BossSwordController>().ThrowVolley(_player.transform,
                new[] { Vector3.forward * 10f, Vector3.forward * 12f, Vector3.forward * 14f }, 2f);
            Assert.That(swords.Length, Is.EqualTo(3));
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(3));
            Assert.That(_boss.GetComponent<MeleeComboPattern>().CanExecute(), Is.True);
            _boss.GetComponent<BossCombatGate>().Block(this);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(6));
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
        public IEnumerator CompletionReplenishesSixFromZeroOnlyAtTransition()
        {
            foreach (BossSword sword in Inventory.Swords) Object.Destroy(sword.gameObject);
            yield return null;
            Assert.That(Inventory.LivingCount, Is.Zero);
            yield return EnterPhaseTwo();
            Assert.That(Inventory.LivingCount, Is.EqualTo(6));
            Object.Destroy(Inventory.Swords[0].gameObject);
            yield return null;
            _boss.GetComponent<PhaseTwoSwordReplenisher>().ReplenishOnce();
            Assert.That(Inventory.LivingCount, Is.EqualTo(5), "2페이즈 전투 중 손실은 재생성하지 않는다.");
        }

        [UnityTest]
        public IEnumerator CompletionPreservesExistingSwordsWhenReplenishing()
        {
            BossSword[] original = Inventory.Swords.ToArray();
            yield return EnterPhaseTwo();
            Assert.That(Inventory.LivingCount, Is.EqualTo(6));
            foreach (BossSword sword in original) Assert.That(Inventory.Swords.Contains(sword), Is.True);
        }

        [UnityTest]
        public IEnumerator CompletionDashRetargetsThreeTimesWithoutLeavingScorch()
        {
            yield return EnterPhaseTwo();
            var pattern = _boss.GetComponent<DashPattern>();
            var motor = _boss.GetComponent<BossDashMotor>();
            var receiver = _player.GetComponent<DashTestReceiver>();
            Set(pattern, "_warningDuration", 0.05f);
            Set(pattern, "_repeatWarningDuration", 0.05f);
            Set(pattern, "_dashDistance", 12f);
            Set(pattern, "_recoveryDuration", 0.2f);
            Set(pattern, "_comboRecoveryDuration", 0.65f);
            Physics.SyncTransforms();

            // 같은 플레이어를 지나치도록 배치해 다음 돌진이 되돌아오는지와 매번의 명중을 확인한다.
            Assert.That(Runner.TryRun(pattern), Is.True);
            var directions = new List<Vector3>();
            bool wasDashing = false;
            float deadline = Time.time + 5f;
            while (Runner.State != BossActionState.Recovery && Time.time < deadline)
            {
                if (motor.IsDashing && !wasDashing)
                    directions.Add(pattern.PlannedPath.Direction);
                wasDashing = motor.IsDashing;
                Assert.That(Object.FindAnyObjectByType<DashScorchField>(), Is.Null);
                yield return null;
            }

            Assert.That(Runner.State, Is.EqualTo(BossActionState.Recovery));
            Assert.That(pattern.RecoveryDuration, Is.EqualTo(0.65f));
            Assert.That(directions.Count, Is.EqualTo(3));
            Assert.That(Vector3.Dot(directions[0], Vector3.forward), Is.GreaterThan(0.99f));
            Assert.That(Vector3.Dot(directions[1], Vector3.back), Is.GreaterThan(0.99f));
            Assert.That(Vector3.Dot(directions[2], Vector3.forward), Is.GreaterThan(0.99f));
            Assert.That(receiver.DamageCalls, Is.EqualTo(3));
            Assert.That(receiver.TotalDamage, Is.EqualTo(6f));
            Assert.That(_boss.GetComponent<DashHitbox>().IsActive, Is.False);
            Assert.That(Object.FindAnyObjectByType<DashScorchField>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator CompletionDashCancellationDuringFollowUpWarningStopsTheCombo()
        {
            yield return EnterPhaseTwo();
            var pattern = _boss.GetComponent<DashPattern>();
            var motor = _boss.GetComponent<BossDashMotor>();
            var groggy = _boss.GetComponent<BossGroggyController>();
            var telegraph = _boss.GetComponentInChildren<DashTelegraphPresenter>(true);
            Transform originalParent = telegraph.transform.parent;
            Set(pattern, "_warningDuration", 0.05f);
            Set(pattern, "_repeatWarningDuration", 0.35f);
            Set(pattern, "_dashDistance", 12f);
            Physics.SyncTransforms();

            Assert.That(Runner.TryRun(pattern), Is.True);
            float deadline = Time.time + 5f;
            // 두 번째 예고는 러너의 실행 구간에 표시된다. 이때 그로기가 남은 돌진과 예고를 정리해야 한다.
            while ((Runner.State != BossActionState.Executing || telegraph.transform.parent != null) &&
                   Time.time < deadline)
                yield return null;

            Assert.That(Runner.State, Is.EqualTo(BossActionState.Executing));
            Assert.That(telegraph.transform.parent, Is.Null);
            Assert.That(motor.IsDashing, Is.False);
            Assert.That(groggy.TryEnterGroggy(2f), Is.True);
            Vector3 stopped = _boss.transform.position;
            yield return new WaitForSeconds(0.45f);

            Assert.That(Runner.State, Is.EqualTo(BossActionState.Groggy));
            Assert.That(Runner.CurrentPattern, Is.Null);
            Assert.That(telegraph.transform.parent, Is.EqualTo(originalParent));
            Assert.That(_boss.GetComponent<DashHitbox>().IsActive, Is.False);
            Assert.That(Vector3.Distance(stopped, _boss.transform.position), Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator CompletionMeleeUsesDifferentSwordsPerStrike()
        {
            yield return EnterPhaseTwo();
            FastMelee();
            Profiles.PhaseTwo.SwingDuration = 0.05f;
            Profiles.PhaseTwo.ComboWarning = 0.05f;
            var used = new HashSet<BossSword>();
            Assert.That(Runner.TryRun(_boss.GetComponent<MeleeComboPattern>()), Is.True);
            float deadline = Time.time + 3f;
            do
            {
                foreach (BossSword sword in Inventory.Swords)
                    if (sword != null && sword.State == BossSwordState.Held) used.Add(sword);
                yield return null;
            } while (Runner.CurrentPattern != null && Time.time < deadline);
            Assert.That(used.Count, Is.EqualTo(3));
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator CompletionSequentialThrowRetargetsAndDoesNotLaunchTogether()
        {
            yield return EnterPhaseTwo();
            var pattern = _boss.GetComponent<SwordThrowPattern>();
            var controller = _boss.GetComponent<BossSwordController>();
            Set(controller, "_throwPreparationCenter", _boss.transform);
            Set(pattern, "_warningDuration", 0.03f);
            Set(pattern, "_repeatWarningDuration", 0.06f);
            Profiles.PhaseTwo.ThrowCount = 3;
            var seen = new HashSet<ThrownBossSword>();
            var embedded = new List<Vector3>();
            Assert.That(Runner.TryRun(pattern), Is.True);
            float deadline = Time.time + 5f;
            do
            {
                ThrownBossSword active = controller.ActiveSword;
                // ActiveSword는 첫 박힌 검을 반환하므로 전체 목록에서 새 비행 검을 찾는다.
                foreach (BossSword sword in Inventory.Swords)
                {
                    var thrown = sword != null ? sword.GetComponent<ThrownBossSword>() : null;
                    if (thrown == null || !thrown.IsFlying || !seen.Add(thrown)) continue;
                    thrown.Embedded += landed =>
                    {
                        embedded.Add(landed.transform.position);
                        _player.transform.position += Vector3.right * 3f;
                    };
                }
                Assert.That(Inventory.Swords.Count(sword => sword != null && sword.State == BossSwordState.Flying), Is.LessThanOrEqualTo(1));
                Assert.That(Inventory.Swords.Count(sword => sword != null && sword.State == BossSwordState.PreparingThrow), Is.LessThanOrEqualTo(1));
                yield return null;
            } while (Runner.CurrentPattern != null && Time.time < deadline);
            Assert.That(seen.Count, Is.EqualTo(3));
            Assert.That(embedded.Count, Is.EqualTo(3));
            Assert.That(embedded[1].x - embedded[0].x, Is.EqualTo(3f).Within(0.15f));
            Assert.That(embedded[2].x - embedded[1].x, Is.EqualTo(3f).Within(0.15f));
            Assert.That(controller.CanThrow, Is.False, "박힌 검이 남으면 새 패턴은 금지한다.");
        }

        [UnityTest]
        public IEnumerator CompletionSequentialThrowEntersGroggyAfterAllLaunchedSwordsBreak()
        {
            yield return EnterPhaseTwo();
            var pattern = _boss.GetComponent<SwordThrowPattern>();
            var controller = _boss.GetComponent<BossSwordController>();
            var groggy = _boss.GetComponent<BossGroggyController>();
            var bossHealth = _boss.GetComponent<BossHealth>();
            float initialHealth = bossHealth.CurrentHealth;
            Set(controller, "_throwPreparationCenter", _boss.transform);
            Set(pattern, "_warningDuration", 0.03f);
            Set(pattern, "_repeatWarningDuration", 0.1f);
            Profiles.PhaseTwo.ThrowCount = 3;

            Assert.That(Runner.TryRun(pattern), Is.True);
            float deadline = Time.time + 5f;
            while ((controller.ActiveSword == null || !controller.ActiveSword.IsEmbedded) && Time.time < deadline)
                yield return null;

            ThrownBossSword first = controller.ActiveSword;
            Assert.That(first != null && first.IsEmbedded, Is.True);
            // 첫 검이 다음 발사 전에 부서져 필드가 비어도 연속 투척은 계속되어야 한다.
            first.GetComponent<BreakableHealth>().ReceiveDamage(1000f);
            Assert.That(controller.HasSword, Is.False);
            Assert.That(controller.OwnsThrowSequence(pattern), Is.True);
            Assert.That(groggy.IsGroggy, Is.False);
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(initialHealth - 100f));

            ThrownBossSword[] embedded = System.Array.Empty<ThrownBossSword>();
            while (Time.time < deadline)
            {
                embedded = Inventory.Swords
                    .Where(sword => sword != null && sword.State == BossSwordState.Embedded)
                    .Select(sword => sword.GetComponent<ThrownBossSword>())
                    .ToArray();
                if (embedded.Length == 2 && !controller.OwnsThrowSequence(pattern)) break;
                yield return null;
            }

            Assert.That(embedded.Length, Is.EqualTo(2));
            Assert.That(controller.OwnsThrowSequence(pattern), Is.False);
            embedded[0].GetComponent<BreakableHealth>().ReceiveDamage(1000f);
            Assert.That(groggy.IsGroggy, Is.False);
            Assert.That(embedded[1].IsEmbedded, Is.True);

            embedded[1].GetComponent<BreakableHealth>().ReceiveDamage(1000f);
            Assert.That(groggy.IsGroggy, Is.True);
            Assert.That(bossHealth.CurrentHealth, Is.EqualTo(initialHealth - 300f));
        }

        [UnityTest]
        public IEnumerator CompletionThrowWithARecalledSwordDoesNotEnterGroggy()
        {
            yield return EnterPhaseTwo();
            var controller = _boss.GetComponent<BossSwordController>();
            var groggy = _boss.GetComponent<BossGroggyController>();
            ThrownBossSword[] thrown = controller.ThrowVolley(_player.transform,
                new[] { Vector3.forward * 10f, Vector3.right * 10f }, 2f);
            Assert.That(thrown.Length, Is.EqualTo(2));

            // 회수로 목록에서 빠진 검도 파괴 조건에는 계속 포함된다.
            thrown[0].Recall();
            float deadline = Time.time + 2f;
            while (thrown[1].IsFlying && Time.time < deadline) yield return null;
            Assert.That(thrown[1].IsEmbedded, Is.True);
            thrown[1].GetComponent<BreakableHealth>().ReceiveDamage(1000f);

            Assert.That(controller.HasSword, Is.False);
            Assert.That(groggy.IsGroggy, Is.False);
        }

        [UnityTest]
        public IEnumerator CompletionSequenceCancellationRestoresReservations()
        {
            yield return EnterPhaseTwo();
            var controller = _boss.GetComponent<BossSwordController>();
            Set(controller, "_throwPreparationCenter", _boss.transform);
            Assert.That(controller.TryBeginThrowSequence(this), Is.True);
            Assert.That(controller.TryBeginThrowSequence(new object()), Is.False);
            Assert.That(controller.PrepareNextThrow(this), Is.EqualTo(1));
            controller.UpdateThrowPreparation(0.5f);
            _boss.GetComponent<BossCombatGate>().Block(this);
            Assert.That(controller.OwnsThrowSequence(this), Is.False);
            Assert.That(Inventory.OrbitingCount, Is.EqualTo(6));
            Assert.That(controller.PrepareNextThrow(this), Is.Zero);
        }

        [UnityTest]
        public IEnumerator CompletionKnockbackMovesCharacterControllerAndHitsWallOnce()
        {
            var actor = new GameObject("Knockback integration actor");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                actor.transform.position = new Vector3(15f, 1f, 0f);
                wall.transform.position = new Vector3(15f, 1f, 2f);
                wall.transform.localScale = new Vector3(5f, 3f, 0.2f);
                var body = actor.AddComponent<CharacterController>();
                var health = actor.AddComponent<DashTestReceiver>();
                var receiver = actor.AddComponent<KnockbackReceiver>();
                // Runtime 테스트에 Assembly-CSharp 참조를 추가하지 않고 실제 어댑터 경계를 검증한다.
                var type = System.Type.GetType("CWH.BossIntegration.PlayerKnockbackMovement, Assembly-CSharp");
                Assert.That(type, Is.Not.Null);
                Component movement = actor.AddComponent(type);
                type.GetMethod("Bind").Invoke(movement, new object[] { body });
                var impact = actor.AddComponent<RoarWallImpact>();
                impact.Arm(_boss.transform, Vector3.forward, 0.5f, 2f, ~0);
                Physics.SyncTransforms();
                receiver.ReceiveKnockBack(Vector3.forward, 20f, 0.4f);
                yield return new WaitForSeconds(0.45f);
                Assert.That(actor.transform.position.z, Is.GreaterThan(0.5f));
                Assert.That(actor.transform.position.z, Is.LessThan(2f));
                Assert.That(health.DamageCalls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(wall);
            }
        }

        [UnityTest]
        public IEnumerator PolishDeathCleansUpSummonsThrownSwordsAndScorch()
        {
            _boss.AddComponent<BossDeathCleanup>();
            var thrown = _boss.GetComponent<BossSwordController>().Throw(_player.transform, Vector3.forward * 10f, 2f);
            var field = _boss.GetComponent<DashScorchFactory>().Spawn(_player.transform, 4f, new ScorchSettings(5f, 0.1f, 1f));
            field.ExtendTo(Vector3.forward * 10f);
#if UNITY_EDITOR
            var minion = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/PhaseOne/PhaseOnePawn.prefab"))
                .GetComponent<ChessMinion>();
            minion.Initialize(_player.transform, 10f, Runner);
            _boss.GetComponent<BossSummonRoster>().Register(minion);
#endif
            _boss.GetComponent<BossHealth>().ReceiveDamage(float.MaxValue);
            yield return null; yield return null;
            Assert.That(_boss == null, Is.True);
            Assert.That(thrown == null, Is.True);
            Assert.That(field == null, Is.True);
#if UNITY_EDITOR
            Assert.That(minion == null, Is.True);
#endif
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
