using System.Collections;
using System.Reflection;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Telegraphs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    /// <summary>Agent 없이 직접 위치를 옮겨 실제 입자 방출을 확인한다.</summary>
    public sealed class BossMovementEffectTests
    {
        private GameObject _root;
        private ParticleSystem _particles;
        private BossMovementEffect _effect;

        [SetUp]
        public void Setup()
        {
            _root = new GameObject("Movement dust test");
            _root.SetActive(false);
            var child = new GameObject("Dust");
            child.transform.SetParent(_root.transform, false);
            _particles = child.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 3f;
            _effect = _root.AddComponent<BossMovementEffect>();
            typeof(BossMovementEffect).GetField("_stepParticle", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_effect, _particles);
            _root.SetActive(true);
        }

        [TearDown]
        public void Cleanup() { if (_root != null) Object.DestroyImmediate(_root); }

        [UnityTest]
        public IEnumerator DirectMovementEmitsAndStandingStillStopsNewParticles()
        {
            yield return null;
            Assert.That(_particles.isEmitting, Is.False);
            _root.transform.position += Vector3.right;
            yield return null;
            Assert.That(_particles.isEmitting, Is.True, "Agent 속도 없이 직접 이동해도 먼지가 나와야 합니다.");
            yield return null;
            Assert.That(_particles.isEmitting, Is.False, "정지하면 새 먼지 방출을 멈춰야 합니다.");
        }

        [UnityTest]
        public IEnumerator VerticalMotionAndDisabledTeleportDoNotEmitFootDust()
        {
            yield return null;
            _root.transform.position += Vector3.up * 2f;
            yield return null;
            Assert.That(_particles.isEmitting, Is.False);
            _effect.enabled = false;
            _root.transform.position += Vector3.right * 20f;
            _effect.enabled = true;
            yield return null;
            Assert.That(_particles.isEmitting, Is.False, "다시 켤 때 이전 위치와의 차이를 이동으로 세면 안 됩니다.");
        }

        [UnityTest]
        public IEnumerator DisablingEffectStopsEmissionAndRetainsExistingDust()
        {
            _particles.Emit(3);
            _root.transform.position += Vector3.right;
            yield return null;
            Assert.That(_particles.isEmitting, Is.True);
            _effect.enabled = false;
            Assert.That(_particles.isEmitting, Is.False);
            Assert.That(_particles.particleCount, Is.GreaterThan(0), "이미 나온 먼지는 강제로 지우지 않습니다.");
        }
    }
}
