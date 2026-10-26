using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
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
    /// <summary>조립된 실제 프리팹을 사용해 패턴 간 상호작용과 페이즈 경계를 검사한다.</summary>
    public sealed class PhaseOneTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private NavMeshData _nav;
        private NavMeshDataInstance _instance;
        private GameObject _boss;
        private GameObject _player;
        private BossPatternRunner _runner;

        [SetUp]
        public void Setup()
        {
            var settings = NavMesh.GetSettingsByIndex(0);
            var sources = new List<NavMeshBuildSource> { new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one), size = new Vector3(80f, 0.2f, 80f) } };
            _nav = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, Vector3.one * 100f), Vector3.zero, Quaternion.identity);
            _instance = NavMesh.AddNavMeshData(_nav);
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/PhaseOne/PhaseOneBoss.prefab");
            Assert.That(prefab, Is.Not.Null, "1페이즈 프리팹이 먼저 생성되어 있어야 합니다.");
            _boss = Object.Instantiate(prefab);
#endif
            _objects.Add(_boss);
            _boss.GetComponent<PhaseOneBrain>().enabled = false;
            _boss.GetComponent<BossMovement>().SetChaseEnabled(false);
            _boss.GetComponent<NavMeshAgent>().Warp(Vector3.zero);
            _runner = _boss.GetComponent<BossPatternRunner>();
            Set(_runner, "_patternInterval", 0f);
            _player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(_player);
            _player.transform.position = new Vector3(0f, 1f, 5f);
            _player.AddComponent<DashTestReceiver>();
            _boss.GetComponent<BossTarget>().SetTarget(_player.transform);
        }
        [TearDown]
        public void Cleanup()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            if (_instance.valid) _instance.Remove();
            if (_nav != null) Object.DestroyImmediate(_nav);
        }
        private static void Set(object owner, string field, object value)
        {
            System.Type type = owner.GetType();
            while (type != null)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                if (info != null) { info.SetValue(owner, value); return; }
                type = type.BaseType;
            }
            Assert.Fail("필드 없음: " + field);
        }
        private void Fast(BossPattern pattern) { Set(pattern, "_warningDuration", 0.04f); Set(pattern, "_recoveryDuration", 0.02f); Set(pattern, "_cooldown", 0f); }
        private IEnumerator FinishPattern(float timeout = 3f)
        {
            float deadline = Time.time + timeout;
            while (_runner.CurrentPattern != null && Time.time < deadline) yield return null;
            Assert.That(_runner.CurrentPattern, Is.Null, "패턴이 제한 시간 안에 종료되어야 합니다.");
        }

        [Test]
        public void SectorRejectsBehindAndDeduplicatesColliders()
        {
            _boss.transform.position = Vector3.zero;
            _player.transform.position = new Vector3(0f, 1f, 3f);
            GameObject extra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            extra.transform.SetParent(_player.transform, false);
            Physics.SyncTransforms();
            BossAttackQuery.ApplySector(_boss.transform, Vector3.zero, Vector3.forward, 6f, 120f, 5f, 20f, ~0, targetRoot: _player.transform);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
            Assert.That(BossAttackQuery.Contains(Vector3.back * 2f, Vector3.forward, 6f, 120f), Is.False);
        }

        [UnityTest]
        public IEnumerator MeleeHitsTwiceButCanDodgeSecondWarning()
        {
            var melee = _boss.GetComponent<MeleeComboPattern>(); Fast(melee);
            Set(melee, "_secondWarning", 0.18f); Set(melee, "_swingDuration", 0.03f);
            Assert.That(_runner.TryRun(melee), Is.True);
            float limit = Time.time + 1f;
            while (_player.GetComponent<DashTestReceiver>().DamageCalls == 0 && Time.time < limit) yield return null;
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
            _player.transform.position = Vector3.right * 30f;
            yield return FinishPattern();
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MeleeHitsStationaryTargetExactlyTwice()
        {
            var melee = _boss.GetComponent<MeleeComboPattern>(); Fast(melee);
            Set(melee, "_secondWarning", 0.04f); Set(melee, "_swingDuration", 0.03f);
            Assert.That(_runner.TryRun(melee), Is.True);
            yield return FinishPattern();
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DodgingFirstWarningDoesNotCancelLockedCombo()
        {
            var melee = _boss.GetComponent<MeleeComboPattern>(); Fast(melee);
            Set(melee, "_warningDuration", 0.15f);
            Set(melee, "_secondWarning", 0.04f); Set(melee, "_swingDuration", 0.03f);
            bool executed = false;
            _runner.StateChanged += state => { if (state == BossActionState.Executing) executed = true; };
            Assert.That(_runner.TryRun(melee), Is.True);
            yield return null; yield return null;
            _player.transform.position = Vector3.right * 30f;
            yield return FinishPattern();
            Assert.That(executed, Is.True);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
        }

        [Test]
        public void AreaWarningKeepsWorldPoseWhenItsOwnerMoves()
        {
            AreaTelegraphPresenter area = _boss.transform.Find("Melee warning").GetComponent<AreaTelegraphPresenter>();
            area.Show(Vector3.forward * 6f, Vector3.forward, 3f);
            Vector3 position = area.transform.position;
            _boss.transform.position += Vector3.right * 10f;
            _boss.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            typeof(AreaTelegraphPresenter).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(area, null);
            Assert.That(Vector3.Distance(area.transform.position, position), Is.LessThan(0.001f));
            Assert.That(Vector3.Dot(area.transform.forward, Vector3.forward), Is.GreaterThan(0.999f));
        }

        [UnityTest]
        public IEnumerator SummonsSameKindPairAndOnlyLastDeathRemovesProtection()
        {
            var summon = _boss.GetComponent<SummonPattern>(); Fast(summon);
            Assert.That(_runner.TryRun(summon), Is.True);
            yield return FinishPattern();
            var roster = _boss.GetComponent<BossSummonRoster>();
            Assert.That(roster.LivingCount, Is.EqualTo(2));
            Assert.That(summon.CanExecute(), Is.False);
            ChessMinion[] minions = Object.FindObjectsByType<ChessMinion>();
            Assert.That(minions.Length, Is.EqualTo(2));
            Assert.That(minions[0].Kind, Is.EqualTo(minions[1].Kind));
            BossHealth health = _boss.GetComponent<BossHealth>();
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(50f));
            minions[0].GetComponent<BreakableHealth>().ReceiveDamage(1000f);
            Assert.That(roster.LivingCount, Is.EqualTo(1));
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(50f));
            _boss.GetComponent<BossGroggyController>().TryEnterGroggy();
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(75f));
            minions[1].GetComponent<BreakableHealth>().ReceiveDamage(1000f);
            Assert.That(roster.LivingCount, Is.Zero);
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(150f));
        }

        [UnityTest]
        public IEnumerator RoarDealsNoDamageAndAlwaysEntersGroggy()
        {
            var roar = _boss.GetComponent<RoarPattern>(); Fast(roar);
            Assert.That(_runner.TryRun(roar), Is.True);
            yield return FinishPattern();
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
            Assert.That(_player.GetComponent<DashTestReceiver>().KnockbackCalls, Is.EqualTo(1));
            Assert.That(_boss.GetComponent<BossGroggyController>().IsGroggy, Is.True);
        }

        [UnityTest]
        public IEnumerator BreakingEmbeddedSwordRewardsOnceAndGroggiesBoss()
        {
            var throwing = _boss.GetComponent<SwordThrowPattern>(); Fast(throwing);
            Assert.That(_runner.TryRun(throwing), Is.True);
            yield return FinishPattern();
            BossSwordController swords = _boss.GetComponent<BossSwordController>();
            Assert.That(swords.HasSword, Is.True);
            Assert.That(swords.ActiveSword.IsEmbedded, Is.True);
            Assert.That(throwing.CanExecute(), Is.False);
            float before = _boss.GetComponent<BossHealth>().CurrentHealth;
            var health = swords.ActiveSword.GetComponent<BreakableHealth>();
            health.ReceiveDamage(1000f); health.ReceiveDamage(1000f);
            Assert.That(_boss.GetComponent<BossHealth>().CurrentHealth, Is.EqualTo(before - 100f));
            Assert.That(_boss.GetComponent<BossGroggyController>().IsGroggy, Is.True);
            Assert.That(swords.HasSword, Is.False);
        }

        [UnityTest]
        public IEnumerator NaturalSwordRecallHasNoRewardAndFlyingSwordRejectsDamage()
        {
            var swords = _boss.GetComponent<BossSwordController>();
            var sword = swords.Throw(_player.transform, _player.transform.position - Vector3.up, 2f);
            Assert.That(sword.GetComponent<BreakableHealth>().ReceiveDamage(1000f), Is.Zero);
            Set(sword, "_flightDuration", 0.02f); Set(sword, "_embeddedDuration", 0.06f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(swords.HasSword, Is.False);
            Assert.That(_boss.GetComponent<BossHealth>().CurrentHealth, Is.EqualTo(1000f));
            Assert.That(_boss.GetComponent<BossGroggyController>().IsGroggy, Is.False);
        }

        [UnityTest]
        public IEnumerator PhaseBoundaryCancelsWarningAndRejectsFurtherPhaseOnePatterns()
        {
            var summon = _boss.GetComponent<SummonPattern>();
            Assert.That(_runner.TryRun(summon), Is.True);
            yield return null; yield return null;
            _boss.GetComponent<BossHealth>().ReceiveDamage(400f);
            Assert.That(_boss.GetComponent<BossPhaseController>().CurrentPhase, Is.EqualTo(2));
            Assert.That(_runner.CurrentPattern, Is.Null);
            Assert.That(_runner.TryRun(summon), Is.False);
            Assert.That(_boss.GetComponent<NavMeshAgent>().isStopped, Is.True);
            Assert.That(_boss.GetComponent<BossSummonRoster>().LivingCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DeathRemovesEmbeddedSwordAndSummonsWithoutBreakReward()
        {
            var summon = _boss.GetComponent<SummonPattern>(); Fast(summon);
            Assert.That(_runner.TryRun(summon), Is.True);
            yield return FinishPattern();
            var swords = _boss.GetComponent<BossSwordController>();
            swords.Throw(_player.transform, Vector3.forward * 4f, 2f);
            _boss.GetComponent<BossHealth>().ReceiveDamage(100000f);
            Assert.That(_boss.GetComponent<BossHealth>().IsDead, Is.True);
            Assert.That(swords.HasSword, Is.False);
            Assert.That(_boss.GetComponent<BossSummonRoster>().LivingCount, Is.Zero);
            Assert.That(_runner.State, Is.EqualTo(BossActionState.Dead));
        }

        [UnityTest]
        public IEnumerator AutomaticBrainStartsEligiblePatterns()
        {
            var brain = _boss.GetComponent<PhaseOneBrain>();
            Set(brain, "_openingDelay", 0f); brain.enabled = true;
            float deadline = Time.time + 1f;
            while (_runner.CurrentPattern == null && Time.time < deadline) yield return null;
            Assert.That(_runner.CurrentPattern, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PawnWarnsBeforeDealingOneHit() => CheckMinion("PhaseOnePawn", false);

        [UnityTest]
        public IEnumerator KnightJumpsToLockedPointAndPlayerCanDodge() => CheckMinion("PhaseOneKnight", true);

        private IEnumerator CheckMinion(string name, bool dodge)
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Members/CWH/PREFABS/PhaseOne/" + name + ".prefab");
            var minion = Object.Instantiate(prefab).GetComponent<ChessMinion>();
            _objects.Add(minion.gameObject);
            _boss.transform.position = Vector3.left * 20f;
            _boss.GetComponent<NavMeshAgent>().Warp(_boss.transform.position);
            _player.transform.position = new Vector3(0f, 1f, dodge ? 6f : 2f);
            minion.Initialize(_player.transform, 0f, _runner);
            float deadline = Time.time + 1f;
            while (!minion.IsAttacking && Time.time < deadline) yield return null;
            Assert.That(minion.IsAttacking, Is.True);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
            if (dodge) _player.transform.position = Vector3.right * 25f;
            bool airborne = false;
            deadline = Time.time + 3f;
            while (minion.IsAttacking && Time.time < deadline)
            {
                if (minion.transform.position.y > 1f) airborne = true;
                yield return null;
            }
            Assert.That(minion.IsAttacking, Is.False);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.EqualTo(dodge ? 0 : 1));
            Assert.That(airborne, Is.EqualTo(dodge));
            Assert.That(minion.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator CancelDuringSwordFlightRecallsWithoutReward()
        {
            var throwing = _boss.GetComponent<SwordThrowPattern>(); Fast(throwing);
            Assert.That(_runner.TryRun(throwing), Is.True);
            var swords = _boss.GetComponent<BossSwordController>();
            float deadline = Time.time + 1f;
            while (!swords.HasSword && Time.time < deadline) yield return null;
            Assert.That(swords.HasSword && swords.ActiveSword.IsFlying, Is.True);
            _runner.CancelCurrentPattern();
            Assert.That(swords.HasSword, Is.False);
            Assert.That(_boss.GetComponent<BossHealth>().CurrentHealth, Is.EqualTo(1000f));
            Assert.That(_boss.GetComponent<BossGroggyController>().IsGroggy, Is.False);
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RoarOutsideRangeStillGroggiesWithoutHittingPlayer()
        {
            _player.transform.position = Vector3.forward * 30f;
            var roar = _boss.GetComponent<RoarPattern>(); Fast(roar);
            Assert.That(_runner.TryRun(roar), Is.True);
            yield return FinishPattern();
            Assert.That(_player.GetComponent<DashTestReceiver>().DamageCalls, Is.Zero);
            Assert.That(_player.GetComponent<DashTestReceiver>().KnockbackCalls, Is.Zero);
            Assert.That(_boss.GetComponent<BossGroggyController>().IsGroggy, Is.True);
        }

        [Test]
        public void PhaseOneContinuesAboveSixtyPercent()
        {
            _boss.GetComponent<BossHealth>().ReceiveDamage(395f);
            Assert.That(_boss.GetComponent<BossPhaseController>().CurrentPhase, Is.EqualTo(1));
            Assert.That(_boss.GetComponent<SummonPattern>().enabled, Is.True);
        }
    }
}
