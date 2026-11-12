#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CWH.SCRIPTS.Cinematics;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Members.CWH.SCRIPTS.Editor
{
    /// <summary>실제 연결된 씬에서 재생/잠금/복귀를 확인한다. 테스트 이동과 체력 변경은 Play Mode 종료 시 폐기된다.</summary>
    [InitializeOnLoad]
    public static class PhaseCutsceneVerification
    {
        private const string ActiveKey = "CWH.KingCutscene.Verifying";
        private const string Request = "Temp/KingPhaseCutscene/verify.request";
        private static readonly List<string> Checks = new List<string>();
        private static BossPhaseCutscene _cutscene;
        private static BossHealth _health;
        private static BossCombatGate _gate;
        private static MeshFilter _mesh;
        private static Mesh _before;
        private static CinemachineCamera _camera;
        private static PlayableDirector _director;
        private static double _start;
        private static double _playStart;
        private static float _gameStart;
        private static int _step;
        private static float _initialDistance;
        private static Vector3 _anchor;

        static PhaseCutsceneVerification()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.delayCall += () =>
            {
                const string viewRequest = "Temp/KingPhaseCutscene/view.request";
                if (File.Exists(viewRequest) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    File.Delete(viewRequest);
                    PhaseCutsceneBuilder.MatchGameplayView();
                }
                const string lensRequest = "Temp/KingPhaseCutscene/lens.request";
                if (File.Exists(lensRequest) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    File.Delete(lensRequest);
                    PhaseCutsceneBuilder.CalibrateLensZoom();
                }
                if (SessionState.GetBool(ActiveKey, false) && EditorApplication.isPlaying) Begin();
                else if (File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    File.Delete(Request);
                    Verify();
                }
            };
        }

        [MenuItem("CWH/Boss/2페이즈 컷신 검증 (Play Mode 자동 종료)")]
        public static void Verify()
        {
            if (SceneManager.GetActiveScene().path != PhaseCutsceneBuilder.ScenePath)
                throw new InvalidOperationException("DEV_SCNE_2 씬을 열어 주세요.");
            // 검증 중 변경은 Play Mode 안에 한정한다. 진입 전의 편집 상태를 먼저 저장해 복원 기준을 만든다.
            if (SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false)) Begin();
            if (change == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Tick;
        }

        private static void Begin()
        {
            EditorApplication.update -= Tick;
            _step = 0;
            Checks.Clear();
            _start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            Checks.Add("PASS " + message);
        }

        private static void Tick()
        {
            try
            {
                if (_step == 0)
                {
                    if (EditorApplication.timeSinceStartup - _start < 2) return;
                    _cutscene = Object.FindAnyObjectByType<BossPhaseCutscene>();
                    Check(_cutscene != null, "scene has the phase cutscene controller");
                    var data = new SerializedObject(_cutscene);
                    var phase = (BossPhaseController)data.FindProperty("_phase").objectReferenceValue;
                    _health = phase.GetComponent<BossHealth>();
                    _gate = phase.GetComponent<BossCombatGate>();
                    var body = (Renderer)data.FindProperty("_body").objectReferenceValue;
                    _mesh = body.GetComponent<MeshFilter>();
                    _before = _mesh.sharedMesh;
                    _camera = (CinemachineCamera)data.FindProperty("_cutsceneCamera").objectReferenceValue;
                    _director = _cutscene.GetComponent<PlayableDirector>();
                    var timeline = (TimelineAsset)_director.playableAsset;
                    Check(timeline.GetOutputTracks().Count() == 7, "all seven Timeline tracks exist");
                    Check(Math.Abs(timeline.duration - 5.5) < 0.02, "Timeline duration is 5.5 seconds");
                    Check(_camera.Follow == null && _camera.LookAt == null && _camera.GetComponents<CinemachineComponentBase>().Length == 0,
                        "cutscene camera has no procedural tracking competing with animation");
                    // 등장 컷신 완료를 먼저 통지한다. 테스트는 현재 씬의 전투 위치에서 시작한다.
                    var intro = (PlayableDirector)data.FindProperty("_intro").objectReferenceValue;
                    intro.Play();
                    intro.Stop();
                    var animator = body.GetComponent<Animator>();
                    if (animator != null) animator.enabled = false;
                    body.transform.localPosition = Vector3.zero;
                    phase.GetComponent<BossPatternRunner>().CancelCurrentPattern();
                    var agent = phase.GetComponent<NavMeshAgent>();
                    Vector3 displaced = phase.transform.position + new Vector3(0.4f, 0f, 0.3f);
                    if (agent != null && agent.isOnNavMesh) agent.Warp(displaced);
                    else phase.transform.position = displaced;
                    phase.transform.rotation = Quaternion.Euler(0, 137, 0);
                    _health.ReceiveDamage(_health.MaxHealth * 0.4f + 0.1f);
                    Check(phase.CurrentPhase == 2 && _cutscene.IsRunning, "health threshold automatically starts cutscene");
                    Check(!_gate.IsOpen, "boss combat is locked during cutscene");
                    Check((_cutscene.transform.position - body.bounds.center).sqrMagnitude < 0.001f,
                        "cutscene anchors at boss CURRENT position after displacement");
                    Check(Mathf.Abs(Mathf.DeltaAngle(_cutscene.transform.eulerAngles.y, 137)) < 0.1f,
                        "cutscene copies only boss starting yaw");
                    Check(_health.ReceiveDamage(100) == 0f, "boss damage is blocked during cutscene");
                    _anchor = _cutscene.transform.position;
                    _initialDistance = new Vector3(0.8f, 0.7f, 3.4f).magnitude;
                    Check(_mesh.sharedMesh == _before, "white king remains until impact signal");
                    _playStart = EditorApplication.timeSinceStartup;
                    _gameStart = Time.time;
                    _step = 1;
                    return;
                }
                double elapsed = Time.time - _gameStart;
                if (_step == 1 && elapsed > 2.1)
                {
                    Check(_cutscene.IsRunning && !_gate.IsOpen, "original two-second transition does not release cutscene lock");
                    Check(_camera.transform.localPosition.magnitude < _initialDistance - 0.3f,
                        $"camera zooms toward boss (time={_director.time:F2}, position={_camera.transform.localPosition}, initial={_initialDistance:F2})");
                    Check(_camera.Lens.FieldOfView < 41f, "lens narrows while charging");
                    var gameplay = (CinemachineVirtualCameraBase)_director.GetReferenceValue(BossPhaseCutscene.GameplayCameraReference, out bool valid);
                    Check(valid && gameplay != null && Quaternion.Angle(_camera.transform.rotation, gameplay.transform.rotation) < 0.5f,
                        "cutscene keeps gameplay camera pitch and yaw despite boss rotation");
                    Check(Mathf.Abs(_camera.transform.localPosition.x) < 0.001f && Mathf.Abs(_camera.transform.localPosition.y) < 0.001f,
                        "zoom follows viewing axis without sideways offsets");
                    var body = (Renderer)new SerializedObject(_cutscene).FindProperty("_body").objectReferenceValue;
                    var output = Camera.main;
                    var lower = output.WorldToViewportPoint(body.bounds.center - Vector3.up * body.bounds.extents.y);
                    var upper = output.WorldToViewportPoint(body.bounds.center + Vector3.up * body.bounds.extents.y);
                    Check(Mathf.Abs(upper.y - lower.y) > 0.4f, "boss fills at least 40 percent of output frame during zoom");
                    Check(Vector3.Distance(_anchor, _cutscene.transform.position) < 0.01f, "cutscene anchor remains fixed");
                    ScreenCapture.CaptureScreenshot("Temp/KingPhaseCutscene/charge.png");
                    _step = 2;
                }
                if (_step == 2 && elapsed > 3.15)
                {
                    Check(_mesh.sharedMesh != _before, "impact Signal reveals black king");
                    Check(_cutscene.transform.Find("Dark fire impact").gameObject.activeSelf, "impact particle is active at reveal");
                    ScreenCapture.CaptureScreenshot("Temp/KingPhaseCutscene/reveal.png");
                    _step = 3;
                }
                if (_step == 3 && elapsed > 6.1)
                {
                    Check(!_cutscene.IsRunning, "natural completion releases cutscene ownership");
                    Check(_gate.IsOpen, "combat resumes after completion");
                    Check(_health.ReceiveDamage(1f) > 0f, "damage multiplier is restored");
                    Check(_director.state != PlayState.Playing, "director stops instead of holding last frame");
                    Check(!_cutscene.transform.Find("Awakened aura").gameObject.activeSelf, "particles deactivate after completion");
                    var brain = (CinemachineBrain)new SerializedObject(_cutscene).FindProperty("_brain").objectReferenceValue;
                    Check(!ReferenceEquals(brain.ActiveVirtualCamera, _camera), "Brain returns to gameplay camera");
                    _cutscene.SendMessage("TestPlay");
                    Check(_cutscene.IsRunning, "preview replay starts");
                    _director.Stop();
                    Check(!_cutscene.IsRunning && _gate.IsOpen, "manual Stop releases combat and cutscene locks");
                    _cutscene.SendMessage("TestPlay");
                    _cutscene.enabled = false;
                    Check(!_cutscene.IsRunning && _gate.IsOpen, "component disable releases locks");
                    Finish(null);
                }
                if (EditorApplication.timeSinceStartup - _playStart > 45)
                    throw new TimeoutException($"Cutscene validation timed out. game={elapsed:F2}, director={_director.time:F2}, scale={Time.timeScale}");
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void Finish(Exception exception)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ActiveKey, false);
            if (exception != null) { Checks.Add("FAIL " + exception); Debug.LogException(exception); }
            File.WriteAllLines("Temp/KingPhaseCutscene/verification.txt", Checks);
            Debug.Log("KING_PHASE_CUTSCENE_VERIFIED " + (exception == null ? "PASS" : "FAIL"));
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
