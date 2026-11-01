#if UNITY_EDITOR
using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Patterns;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement;
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
        public IEnumerator SavedDemoBootsWithTargetNavigationAndRequiredPatterns()
        {
            _loaded = EditorSceneManager.LoadSceneInPlayMode("Assets/Members/CWH/SCENCES/PhaseOne_Playground.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null; yield return null;
            BossTarget target = null;
            foreach (GameObject root in _loaded.GetRootGameObjects())
                if (root.TryGetComponent(out BossTarget found)) target = found;
            Assert.That(target, Is.Not.Null);
            Assert.That(target.HasTarget, Is.True);
            Assert.That(target.GetComponents<ChessKingGridMovement>().Length, Is.EqualTo(1));
            var movementController = target.GetComponent<Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Movement.BossMovementStateController>();
            var serialized = new UnityEditor.SerializedObject(movementController);
            Assert.That(serialized.FindProperty("_movementDriver").objectReferenceValue,
                Is.SameAs(target.GetComponent<ChessKingGridMovement>()), "테스트 씬도 실제 그리드 이동기를 제어해야 합니다.");
            Assert.That(target.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
            // 필수 다섯 종류를 검사하되, 별도로 추가한 기술 변형까지 금지하지 않는다.
            Assert.That(target.GetComponent<MeleeComboPattern>(), Is.Not.Null);
            Assert.That(target.GetComponent<SummonPattern>(), Is.Not.Null);
            Assert.That(target.GetComponent<DashPattern>(), Is.Not.Null);
            Assert.That(target.GetComponent<SwordThrowPattern>(), Is.Not.Null);
            Assert.That(target.GetComponent<RoarPattern>(), Is.Not.Null);
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
