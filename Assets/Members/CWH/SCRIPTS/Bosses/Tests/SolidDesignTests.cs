using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.AI;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Dash;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    // 새로운 행동을 연결해도 공통 FSM이 준비/경고/완료 약속만으로 실행하는지 확인한다.
    public sealed class CustomMinionAttackForTests : ChessMinionAttack
    {
        public int PrepareCalls;
        public int AttackCalls;
        public override float TriggerDistance => 5f;
        protected override bool TryPrepare() { PrepareCalls++; return true; }
        public override void ShowWarning() => Warning.Show(transform.position, Vector3.forward, BaseRadius);
        public override void BeginAttack() { AttackCalls++; Warning.Hide(); ApplyDamage(transform.position, Vector3.forward, 5f, 360f); }
        public override bool Tick(float deltaTime) => true;
    }

    public sealed class SolidDesignTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private NavMeshData _nav;
        private NavMeshDataInstance _navInstance;
        private GameObject Track(string name) { var go = new GameObject(name); _objects.Add(go); return go; }
        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        [TearDown]
        public void Cleanup()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            if (_navInstance.valid) _navInstance.Remove();
            if (_nav != null) Object.DestroyImmediate(_nav);
        }

        [Test]
        public void MotionCanDetermineTheHitWithoutAnyPresentationObject()
        {
            var motion = new SwordSwingMotion();
            Assert.That(motion.HasCrossedFront(0f), Is.False);
            Assert.That(motion.HasCrossedFront(1f), Is.True);
            Assert.That(motion.Angle(0f, 0), Is.EqualTo(-100f));
            Assert.That(motion.Angle(1f, 0), Is.EqualTo(80f));
            Assert.That(motion.Angle(1f, 1), Is.EqualTo(-80f));
        }

        [Test]
        public void OneBindingRestoresBothHandAndFlightOwnership()
        {
            var parent = Track("Owner");
            var hand = Track("Hand");
            var root = Track("Sword");
            root.transform.SetParent(parent.transform, false);
            root.transform.localPosition = new Vector3(1f, 2f, 3f);
            root.transform.localScale = Vector3.one * 2f;
            BossSword sword = root.AddComponent<BossSword>();
            Assert.That(sword.TryHold(), Is.True);
            Assert.That(sword.PoseBinding.TryCapture(), Is.True);
            root.transform.SetParent(hand.transform, false);
            root.transform.localPosition = Vector3.zero;
            sword.PoseBinding.Restore();
            Assert.That(sword.State, Is.EqualTo(BossSwordState.Orbiting));
            Assert.That(root.transform.parent, Is.EqualTo(parent.transform));
            Assert.That(root.transform.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one * 2f));
            Assert.That(sword.TryLaunch(), Is.True);
            sword.PoseBinding.TryCapture();
            root.transform.SetParent(null, true);
            root.transform.position = Vector3.forward * 30f;
            sword.PoseBinding.Restore();
            Assert.That(sword.State, Is.EqualTo(BossSwordState.Orbiting));
            Assert.That(root.transform.parent, Is.EqualTo(parent.transform));
        }

        [Test]
        public void FailedAttachmentDoesNotReleaseAnotherOwnersBinding()
        {
            BossSword sword = Track("Sword").AddComponent<BossSword>();
            BossCombatPresentation presentation = Track("Boss").AddComponent<BossCombatPresentation>();
            Set(presentation, "_weaponPivot", Track("Pivot").transform);
            sword.PoseBinding.TryCapture();
            Assert.That(presentation.TryAttachSword(sword.transform), Is.False);
            presentation.DetachSword();
            Assert.That(sword.PoseBinding.IsCaptured, Is.True);
        }

        [Test]
        public void ProfilesResolvePhaseWithoutKnowingAnyTransitionOrAppearanceComponent()
        {
            GameObject root = Track("Boss");
            BossHealth health = root.AddComponent<BossHealth>();
            root.AddComponent<BossPhaseController>();
            ChessCombatProfiles profiles = root.AddComponent<ChessCombatProfiles>();
            Assert.That(profiles.GetMelee(0.15f, 0.7f).Strikes, Is.EqualTo(2));
            health.ReceiveDamage(400f);
            Assert.That(profiles.GetMelee(0.15f, 0.7f).Strikes, Is.EqualTo(3));
            Assert.That(root.GetComponent<PhaseTwoEncounter>(), Is.Null);
        }

        private sealed class RecordingScorchVisual : IDashScorchVisual
        {
            public Vector3 LastEnd;
            public void SetSegment(Vector3 start, Vector3 end, float width) => LastEnd = end;
        }

        [Test]
        public void DamageFieldAcceptsAnAlternativeVisualWithoutCreatingAMesh()
        {
            GameObject owner = Track("Owner");
            owner.AddComponent<BossHealth>();
            var visual = new RecordingScorchVisual();
            var field = Track("Field").AddComponent<DashScorchField>();
            field.Initialize(owner.transform, null, 4f, new ScorchSettings(5f, 0.75f, 10f), visual);
            field.ExtendTo(Vector3.forward * 10f);
            Assert.That(visual.LastEnd, Is.EqualTo(Vector3.forward * 10f));
            Assert.That(field.GetComponent<MeshFilter>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator NewAttackImplementationRunsThroughUnchangedMinionFsm()
        {
            _nav = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one), size = new Vector3(30f, 0.2f, 30f) }
            }, new Bounds(Vector3.zero, Vector3.one * 40f), Vector3.zero, Quaternion.identity);
            _navInstance = NavMesh.AddNavMeshData(_nav);
            GameObject root = Track("Custom minion");
            root.SetActive(false);
            CustomMinionAttackForTests attack = root.AddComponent<CustomMinionAttackForTests>();
            ChessMinion minion = root.AddComponent<ChessMinion>();
            Set(minion, "_attack", attack);
            Set(minion, "_warningTime", 0.02f);
            root.SetActive(true);
            root.GetComponent<NavMeshAgent>().Warp(Vector3.zero);
            var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(player);
            player.transform.position = new Vector3(0f, 1f, 2f);
            DashTestReceiver receiver = player.AddComponent<DashTestReceiver>();
            minion.Initialize(player.transform, 0f);
            float deadline = Time.time + 1f;
            while (receiver.DamageCalls == 0 && Time.time < deadline) yield return null;
            Assert.That(attack.PrepareCalls, Is.EqualTo(1));
            Assert.That(attack.AttackCalls, Is.EqualTo(1));
            Assert.That(receiver.DamageCalls, Is.EqualTo(1));
        }
    }
}
