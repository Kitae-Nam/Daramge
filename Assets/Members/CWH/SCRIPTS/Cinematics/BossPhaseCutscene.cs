using System.Collections;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace CWH.SCRIPTS.Cinematics
{
    /// <summary>
    /// 페이즈 이벤트를 컷신 재생으로 연결한다. 구도/이펙트/외형 공개 시점은 Timeline이 소유하고,
    /// 이 컴포넌트는 현재 위치 배치, 전투 잠금, 종료 복원만 담당한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayableDirector))]
    public sealed class BossPhaseCutscene : MonoBehaviour
    {
        public const string GameplayCameraReference = "PhaseTwo.GameplayCamera";
        [SerializeField] private BossPhaseController _phase;
        [SerializeField] private BossCombatGate _combatGate;
        [SerializeField] private BossHealth _health;
        [SerializeField] private Renderer _body;
        [SerializeField] private ChessPhaseAppearance _appearance;
        [SerializeField] private PlayableDirector _intro;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineVirtualCameraBase _fallbackGameplayCamera;
        [SerializeField] private CinemachineCamera _cutsceneCamera;
        [SerializeField] private UnityEvent _started = new UnityEvent();
        [SerializeField] private UnityEvent _finished = new UnityEvent();
        private PlayableDirector _director;
        private Coroutine _pending;
        private bool _hasPlayed;
        private bool _ownsLock;
        private bool _inputLocked;
        public bool IsRunning => _ownsLock;

        private void Awake() => _director = GetComponent<PlayableDirector>();

        private void OnEnable()
        {
            _director.stopped += OnStopped;
            if (_phase != null) _phase.PhaseChanged += OnPhaseChanged;
        }

        private void OnPhaseChanged(int phase)
        {
            if (phase == 0) { Cancel(); return; }
            if (phase != 2 || _hasPlayed) return;
            _hasPlayed = true;
            // 페이즈 전환의 기본 2초 타이머와 별도의 소유자로 잠근다. 먼저 끝난 잠금이 컷신을 풀지 못한다.
            _ownsLock = true;
            _combatGate.Block(this);
            _health.SetDamageMultiplier(this, 0f);
            _pending = StartCoroutine(PlayAfterIntro());
        }

        private IEnumerator PlayAfterIntro()
        {
            // 등장 연출 중에 강제로 체력을 낮춰도 두 Timeline이 같은 Brain을 동시에 조종하지 않는다.
            while (_intro != null && _intro.state == PlayState.Playing) yield return null;
            _pending = null;
            if (!_ownsLock) yield break;
            AlignToBoss();
            var gameplay = _brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
            // 등장 Timeline이 방금 끝난 프레임에는 Brain이 아직 등장용 Shot을 가리킬 수 있다.
            if (gameplay == null || gameplay == _cutsceneCamera ||
                (_intro != null && gameplay.transform.IsChildOf(_intro.transform)))
                gameplay = _fallbackGameplayCamera;
            // 이펙트 기준점은 보스 방향을 유지하고 카메라 리그만 플레이어 시점으로 고정한다.
            // 월드 회전을 한 번 복사하므로 Follow나 보스 회전이 컷신 구도에 다시 개입하지 않는다.
            _cutsceneCamera.transform.parent.rotation = gameplay.transform.rotation;
            _director.SetReferenceValue(GameplayCameraReference, gameplay);
            _cutsceneCamera.PreviousStateIsValid = false;
            _started.Invoke();
            _inputLocked = true;
            _director.time = 0d;
            _director.Play();
        }

        /// <summary>보스 높이를 한 단위로 사용해 소품과 카메라가 함께 맞는다. 시작 자세를 한 번만 복사해 회전 피드백을 막는다.</summary>
        public void AlignToBoss()
        {
            if (_phase == null || _body == null) return;
            float height = Mathf.Max(0.5f, _body.bounds.size.y);
            transform.SetPositionAndRotation(_body.bounds.center,
                Quaternion.Euler(0f, _phase.transform.eulerAngles.y, 0f));
            transform.localScale = Vector3.one * height;
        }

        private void OnStopped(PlayableDirector director) => Release();

        private void Cancel()
        {
            if (_pending != null) StopCoroutine(_pending);
            _pending = null;
            if (_director != null && _director.state == PlayState.Playing) _director.Stop();
            Release();
        }

        private void Release()
        {
            if (!_ownsLock) return;
            _ownsLock = false;
            // 중간 Stop/비활성화에서도 외형과 입력, 무적, 전투 잠금이 남지 않게 같은 경로로 복구한다.
            _appearance?.RevealPhaseTwo();
            _health.RemoveDamageMultiplier(this);
            if (_inputLocked) _finished.Invoke();
            _inputLocked = false;
            _combatGate.Unblock(this);
        }

        [ContextMenu("테스트/2페이즈 컷신 재생 (Play Mode)")]
        private void TestPlay()
        {
            if (Application.isPlaying && !_ownsLock) { _hasPlayed = false; OnPhaseChanged(2); }
        }

        private void OnDisable()
        {
            if (_phase != null) _phase.PhaseChanged -= OnPhaseChanged;
            Cancel();
            if (_director != null) _director.stopped -= OnStopped;
        }
    }
}
