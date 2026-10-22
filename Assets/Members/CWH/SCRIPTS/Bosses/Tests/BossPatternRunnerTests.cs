using System.Collections;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    // 테스트용 패턴은 공격 대신 실행/정리 횟수를 기록해 공통 흐름과 중단 계약을 검증한다.
    public sealed class RecordingBossPattern : BossPattern
    {
        public int Warnings;
        public int WarningEnds;
        public int Executions;
        public int Cleanups;
        public int Disposals;
        public bool GroggyDuringExecution;

        public override void BeginWarning() => Warnings++;
        public override void EndWarning() => WarningEnds++;
        public override IEnumerator Execute()
        {
            try
            {
                Executions++;
                if (GroggyDuringExecution) GetComponent<BossGroggyController>().TryEnterGroggy(1f);
                yield return new WaitForSeconds(0.1f);
            }
            finally { Disposals++; }
        }
        public override void Cleanup() => Cleanups++;
    }

    public sealed class BossPatternRunnerTests
    {
        private GameObject _boss;
        private BossPatternRunner _runner;
        private RecordingBossPattern _pattern;

        [SetUp]
        public void SetUp()
        {
            _boss = new GameObject("공용 보스 테스트");
            _runner = _boss.AddComponent<BossPatternRunner>();
            _pattern = _boss.AddComponent<RecordingBossPattern>();
            // 시간을 줄여도 경고/공격/후딜 순서는 그대로 검증한다.
            SetField(typeof(BossPattern), _pattern, "_warningDuration", 0.02f);
            SetField(typeof(BossPattern), _pattern, "_recoveryDuration", 0.02f);
            SetField(typeof(BossPattern), _pattern, "_cooldown", 0f);
            SetField(typeof(BossPatternRunner), _runner, "_patternInterval", 0.5f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_boss);

        [UnityTest]
        public IEnumerator CompletesInOrderAndEnforcesRestInterval()
        {
            var states = new System.Collections.Generic.List<BossActionState>();
            _runner.StateChanged += states.Add;
            Assert.That(_runner.TryRun(_pattern), Is.True);
            Assert.That(_runner.TryRun(_pattern), Is.False);
            yield return WaitForState(BossActionState.Idle, true);
            CollectionAssert.AreEqual(new[] { BossActionState.Warning, BossActionState.Executing,
                BossActionState.Recovery, BossActionState.Idle }, states);
            Assert.That(_pattern.Warnings, Is.EqualTo(1));
            Assert.That(_pattern.WarningEnds, Is.EqualTo(1));
            Assert.That(_pattern.Disposals, Is.EqualTo(1));
            Assert.That(_pattern.Cleanups, Is.EqualTo(1));
            Assert.That(_runner.TryRun(_pattern), Is.False);
        }

        [UnityTest]
        public IEnumerator GroggyDuringWarningCancelsAttackAndRestoresDamageAfterExit()
        {
            _runner.TryRun(_pattern);
            yield return WaitForState(BossActionState.Warning);
            var groggy = _boss.GetComponent<BossGroggyController>();
            var health = _boss.GetComponent<BossHealth>();
            Assert.That(groggy.TryEnterGroggy(1f), Is.True);
            Assert.That(groggy.TryEnterGroggy(0.1f), Is.True);
            Assert.That(groggy.RemainingTime, Is.EqualTo(1f));
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(150f));
            Assert.That(_runner.State, Is.EqualTo(BossActionState.Groggy));
            Assert.That(_runner.TryRun(_pattern), Is.False);
            Assert.That(_pattern.Executions, Is.Zero);
            Assert.That(_pattern.WarningEnds, Is.EqualTo(1));
            Assert.That(_pattern.Cleanups, Is.EqualTo(1));
            groggy.ExitGroggy();
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(100f));
            Assert.That(_runner.State, Is.EqualTo(BossActionState.Idle));
        }

        [UnityTest]
        public IEnumerator GroggyInsideExecuteDisposesIteratorAndSkipsRecovery()
        {
            _pattern.GroggyDuringExecution = true;
            _runner.TryRun(_pattern);
            yield return WaitForState(BossActionState.Groggy);
            Assert.That(_runner.CurrentPattern, Is.Null);
            Assert.That(_pattern.Disposals, Is.EqualTo(1));
            Assert.That(_pattern.Cleanups, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DeathDuringExecutionCancelsAndPreventsNewPatterns()
        {
            _runner.TryRun(_pattern);
            yield return WaitForState(BossActionState.Executing);
            _boss.GetComponent<BossHealth>().ReceiveDamage(float.MaxValue);
            Assert.That(_runner.State, Is.EqualTo(BossActionState.Dead));
            Assert.That(_pattern.Disposals, Is.EqualTo(1));
            Assert.That(_pattern.Cleanups, Is.EqualTo(1));
            Assert.That(_runner.TryRun(_pattern), Is.False);
        }

        [UnityTest]
        public IEnumerator DisablingRunnerRemovesWarningAndCleansPattern()
        {
            _runner.TryRun(_pattern);
            yield return WaitForState(BossActionState.Warning);
            _runner.enabled = false;
            Assert.That(_runner.CurrentPattern, Is.Null);
            Assert.That(_pattern.WarningEnds, Is.EqualTo(1));
            Assert.That(_pattern.Cleanups, Is.EqualTo(1));
        }

        private IEnumerator WaitForState(BossActionState state, bool waitForCompletion = false)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            do
            {
                yield return null;
                if (Time.realtimeSinceStartup > deadline) Assert.Fail("보스 상태 전환 시간 초과");
            }
            while (_runner.State != state || (waitForCompletion && _runner.CurrentPattern != null));
        }

        private static void SetField(System.Type type, object target, string name, object value)
            => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
