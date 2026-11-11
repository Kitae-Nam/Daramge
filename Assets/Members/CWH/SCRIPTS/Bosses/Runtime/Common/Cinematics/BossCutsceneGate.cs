using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Combat;
using UnityEngine;
using UnityEngine.Playables;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Cinematics
{
    /// <summary>명시적으로 연결한 컷신의 재생 상태를 전투 잠금으로 바꾼다. 다른 씬의 Timeline은 감시하지 않는다.</summary>
    [DefaultExecutionOrder(-300), RequireComponent(typeof(BossCombatGate))]
    public sealed class BossCutsceneGate : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        private BossCombatGate _gate;
        private bool _hasPlayed;
        private void Awake() => _gate = GetComponent<BossCombatGate>();
        private void OnEnable()
        {
            if (_director == null) return;
            _director.played += OnPlayed;
            _director.stopped += OnStopped;
            _hasPlayed = _director.state == PlayState.Playing || _director.time > 0d;
            // Director의 Start보다 먼저 잠가 Play On Awake 첫 프레임의 공격도 막는다.
            if (_director.isActiveAndEnabled && _director.playableAsset != null &&
                (_director.playOnAwake || _director.state == PlayState.Playing)) _gate.Block(this);
        }
        private void OnPlayed(PlayableDirector director) { _hasPlayed = true; _gate.Block(this); }
        private void OnStopped(PlayableDirector director) { _hasPlayed = false; _gate.Unblock(this); }
        private void Update()
        {
            if (_director == null || !_director.isActiveAndEnabled) { _gate.Unblock(this); return; }
            // Hold 완료는 stopped 알림 없이 Paused가 될 수 있다. 중간 Pause는 컷신으로 유지한다.
            if (_hasPlayed && _director.state != PlayState.Playing &&
                _director.extrapolationMode != DirectorWrapMode.Loop &&
                _director.time >= _director.duration - 0.001) OnStopped(_director);
        }
        private void OnDisable()
        {
            if (_director != null) { _director.played -= OnPlayed; _director.stopped -= OnStopped; }
            if (_gate != null) _gate.Unblock(this);
        }
    }
}
