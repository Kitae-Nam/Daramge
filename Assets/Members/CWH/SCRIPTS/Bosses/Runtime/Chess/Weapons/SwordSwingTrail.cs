using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Weapons
{
    /// <summary>
    /// 검의 잔상만 제어한다. 직접 만든 파티클 트레일과 TrailRenderer를 모두 지원하며,
    /// 순간 장착/회수 때 이전 좌표가 긴 선으로 이어지지 않도록 기록을 지운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SwordSwingTrail : MonoBehaviour
    {
        private TrailRenderer[] _trails;
        private ParticleSystem[] _particles;
        public bool IsEmitting { get; private set; }

        private void Awake()
        {
            _trails = GetComponentsInChildren<TrailRenderer>(true);
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (TrailRenderer trail in _trails)
                if (trail != null) trail.autodestruct = false;
            foreach (ParticleSystem particle in _particles)
            {
                if (particle == null) continue;
                var main = particle.main;
                main.playOnAwake = false;
            }
            StopAndClear();
        }

        public void Begin()
        {
            StopAndClear();
            IsEmitting = true;
            foreach (TrailRenderer trail in _trails)
                if (trail != null) trail.emitting = true;
            foreach (ParticleSystem particle in _particles)
            {
                if (particle == null) continue;
                var main = particle.main;
                main.playOnAwake = false;
                particle.Play(false);
            }
        }

        // 정상 베기 끝에는 이미 생긴 입자와 띠를 수명에 따라 사라지게 한다.
        public void End()
        {
            IsEmitting = false;
            if (_trails != null)
                foreach (TrailRenderer trail in _trails)
                    if (trail != null) trail.emitting = false;
            if (_particles != null)
                foreach (ParticleSystem particle in _particles)
                    if (particle != null) particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        public void StopAndClear()
        {
            End();
            if (_trails != null)
                foreach (TrailRenderer trail in _trails)
                    if (trail != null) trail.Clear();
            if (_particles != null)
                foreach (ParticleSystem particle in _particles)
                    if (particle != null) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable() => StopAndClear();
    }
}
