#if UNITY_EDITOR
using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    /// <summary>사용자가 여는 최종 씬 자체의 연결과 NavMesh 등록 순서를 검사한다.</summary>
    public sealed class PhaseOneDemoSceneTests
    {
        private Scene _loaded;
        [UnityTest]
        public IEnumerator SavedDemoBootsWithTargetNavigationAndFivePatterns()
        {
            _loaded = EditorSceneManager.LoadSceneInPlayMode("Assets/Members/CWH/SCENCES/PhaseOne_Playground.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null; yield return null;
            BossTarget target = null;
            foreach (GameObject root in _loaded.GetRootGameObjects())
                if (root.TryGetComponent(out BossTarget found)) target = found;
            Assert.That(target, Is.Not.Null);
            Assert.That(target.HasTarget, Is.True);
            Assert.That(target.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
            Assert.That(target.GetComponents<BossPattern>().Length, Is.EqualTo(5));
            Assert.That(target.GetComponent<PhaseOneBrain>().enabled, Is.True);
            Assert.That(target.GetComponent<BossPhaseController>().CurrentPhase, Is.EqualTo(1));
            var runner = target.GetComponent<BossPatternRunner>();
            float deadline = Time.time + 5f;
            while (runner.CurrentPattern == null && Time.time < deadline) yield return null;
            Assert.That(runner.CurrentPattern, Is.Not.Null, "완성 씬의 자동 AI가 실제 공격을 시작해야 합니다.");
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_loaded.IsValid() && _loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(_loaded);
        }
    }
}
#endif
