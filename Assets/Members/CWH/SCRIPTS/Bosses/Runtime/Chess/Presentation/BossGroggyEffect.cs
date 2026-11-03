using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using UnityEngine;

namespace Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Chess.Presentation
{
    [RequireComponent(typeof(BossGroggyController))]
    public class BossGroggyEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _ptc;
        
        private BossGroggyController _groggy;

        private void Awake()
        {
            _groggy = GetComponent<BossGroggyController>();
        }

        private void OnEnable()
        {
            _groggy.Entered += Play;
            _groggy.Exited += Clear;

            if (_groggy.IsGroggy) Play();
            else Clear();
        }

        private void OnDisable()
        {
            _groggy.Entered -= Play;
            _groggy.Exited -= Clear;
            Clear();
        }

        private void Play()
        {
            if (_ptc == null) return ;
            
            Clear();
            _ptc.Play(true);
        }
        private void Stop()
        {
            if (_ptc == null) return ;
            
            _ptc.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        private void Clear()
        {
         
            if (_ptc == null) return ;
            _ptc.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }   
            
    }
}
