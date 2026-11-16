#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CWH.Verification
{
    /// <summary>
    /// DEV_SCNE_3의 실제 직렬화 결과와 Cinemachine 신호를 검사한다.
    /// 씬을 저장하거나 피해를 주지 않으므로 검증 자체가 작업 내용을 변경하지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class DevScene3HitFeedbackVerification
    {
        private const string RequestPath = "Assets/Members/CWH/Validation~/DevScene3HitFeedback.request";
        private const string ResultPath = "Assets/Members/CWH/Validation~/DevScene3HitFeedback.result.txt";

        static DevScene3HitFeedbackVerification()
        {
            // 실행 중인 에디터에서는 미리보기 씬만 열어 사용자의 활성 씬과 미저장 작업을 보존한다.
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
                File.Delete(RequestPath);
                Run();
            };
        }

        [MenuItem("CWH/검증/DEV_SCNE_3 피격 카메라")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("피격 카메라 검증은 Play Mode 밖에서 실행해 주세요.");
                return;
            }
            Scene preview = default;
            float previousTime = CinemachineCore.CurrentTimeOverride;
            var manager = CinemachineImpulseManager.Instance;
            bool previousIgnoreTimeScale = manager.IgnoreTimeScale;
            try
            {
                preview = EditorSceneManager.OpenPreviewScene("Assets/Members/CWH/SCENCES/DEV_SCNE_3.unity");
                var roots = preview.GetRootGameObjects();
                var players = roots.SelectMany(root => root.GetComponentsInChildren<MMF_Player>(true));
                var hits = players.SelectMany(player => player.FeedbacksList
                    .OfType<MMF_CinemachineImpulse>())
                    .Where(feedback => feedback.m_ImpulseDefinition.ImpulseChannel == 2).ToArray();
                Require(hits.Length == 6, "Six scene feedbacks must use hit channel 2.");
                foreach (var hit in hits)
                {
                    var definition = hit.m_ImpulseDefinition;
                    Require(definition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                        "Hit signal must use a fixed-duration uniform impulse.");
                    Require(definition.ImpulseShape == CinemachineImpulseDefinition.ImpulseShapes.Bump,
                        "Hit signal must use the short Bump shape.");
                    Require(Mathf.Approximately(definition.ImpulseDuration, 0.1f), "Hit duration must be 0.1s.");
                    Require(hit.Velocity == new Vector3(0f, -0.18f, 0f), "Hit strength must match scene tuning.");
                }

                var cameras = roots.SelectMany(root => root.GetComponentsInChildren<CinemachineCamera>(true)).ToArray();
                foreach (var camera in cameras)
                {
                    var listeners = camera.GetComponents<CinemachineImpulseListener>();
                    var hitListeners = listeners.Where(listener => (listener.ChannelMask & 2) != 0).ToArray();
                    Require(hitListeners.Length == 1, camera.name + ": exactly one hit listener is required.");
                    var listener = hitListeners[0];
                    Require(Mathf.Approximately(listener.Gain, 1f), camera.name + ": hit gain must be 1.");
                    Require(Mathf.Approximately(listener.ReactionSettings.AmplitudeGain, 0f),
                        camera.name + ": hit reactions must not add a lingering shake.");
                }

                // 실제 Cinemachine 이벤트를 시간별로 샘플링해 피격 채널 격리와 종료 시점을 검사한다.
                manager.Clear();
                manager.IgnoreTimeScale = false;
                CinemachineCore.CurrentTimeOverride = 100f;
                hits[0].m_ImpulseDefinition.CreateEvent(Vector3.zero, hits[0].Velocity);
                CinemachineCore.CurrentTimeOverride = 100.05f;
                manager.GetImpulseAt(Vector3.zero, false, 2, out Vector3 duringHit, out _);
                Require(duringHit.sqrMagnitude > 0.000001f, "Hit must produce a visible nonzero signal.");
                manager.GetImpulseAt(Vector3.zero, false, 1, out Vector3 cinematicSignal, out Quaternion cinematicRotation);
                Require(cinematicSignal.sqrMagnitude < 0.000001f && Quaternion.Angle(cinematicRotation, Quaternion.identity) < 0.001f,
                    "Hit must not activate cinematic channel 1.");
                CinemachineCore.CurrentTimeOverride = 100.12f;
                manager.GetImpulseAt(Vector3.zero, false, 2, out Vector3 afterHit, out Quaternion afterRotation);
                Require(afterHit.sqrMagnitude < 0.000001f && Quaternion.Angle(afterRotation, Quaternion.identity) < 0.001f,
                    "Hit signal must end before 0.12 seconds.");
                string result = "DEV_SCNE_3_HIT_FEEDBACK_PASS: " + hits.Length + " feedbacks, " + cameras.Length
                    + " cameras; nonzero bump, channel isolation and 0.12s cutoff verified.";
                File.WriteAllText(ResultPath, result);
                Debug.Log(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                File.WriteAllText(ResultPath, "FAIL: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                manager.Clear();
                manager.IgnoreTimeScale = previousIgnoreTimeScale;
                CinemachineCore.CurrentTimeOverride = previousTime;
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
