using System;
using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.CoreSystem.FadeSystem;
using CHG._02_Scripts.Player.FSM;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player.Respawn
{
    public class PlayerRespawnModule : MonoBehaviour, IModule, IRespawnable
    {
        private const float FALL_DAMAGE = 1f;
        
        [SerializeField] private float deathRespawnDelay = 2f;
        [SerializeField] private float deathFadeDuration = 0.6f;
        [SerializeField] private float fallFadeDuration = 0.3f;

        private PlayerController _player;
        private IWarpMovement _warp;
        private SafeGroundTracker _safeGround;
        private RespawnArea _currentArea;
        
        public bool IsRespawning { get; private set; }

      
        public void Initialize(ModuleOwner owner)
        {
            _player = owner as PlayerController;
            _warp = owner.GetModule<IWarpMovement>();
            _safeGround = owner.GetModule<SafeGroundTracker>();
            Debug.Assert(_player != null, $"owner is not PlayerController : {gameObject.name}");
            Debug.Assert(_warp != null, $"IWarpMovement is null : {gameObject.name}");
            Debug.Assert(_safeGround != null, $"SafeGroundTracker is null : {gameObject.name}");

            _player.OnDeath.AddListener(HandleDeath);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnDeath.RemoveListener(HandleDeath);
        }

        public void EnterArea(RespawnArea area)
        {
            _currentArea = area;
        }

        public void ExitArea(RespawnArea area)
        {
            if (_currentArea == area)
                _currentArea = null;
        }

        public void RespawnFromFall()
        {
            if (IsRespawning || _player.IsDead) return;
            StartCoroutine(RespawnCoroutine(_safeGround.LastSafePosition, null, false)); 
        }

        private void HandleDeath()
        {
            StopAllCoroutines();
            Vector3 position = _currentArea != null ? _currentArea.RespawnPosition : _safeGround.LastSafePosition;
            StartCoroutine(RespawnCoroutine(position, _currentArea, true));
        }

        private IEnumerator RespawnCoroutine(Vector3 position, RespawnArea area, bool isDeath) 
        {
            IsRespawning = true;
            _player.PlayerInput.DisableInput();
            _player.AddStatus(AgentStatus.Invincible);

            float fadeDuration = isDeath ? deathFadeDuration : fallFadeDuration;
            if (isDeath)                                                        
            {                                                                   
                _player.ChangeState(PlayerState.DEAD, 0.1f);                    
                yield return new WaitForSeconds(deathRespawnDelay);              
            }                                                                   

            yield return FadeCoroutine(1f, fadeDuration);

            _warp.Warp(position);
            if (area != null)
            {
                area.NotifyRespawned();
                _currentArea = null;
            }
            if (isDeath)
                _player.Revive();
            _player.ChangeState(PlayerState.IDLE, 0f);

            yield return FadeCoroutine(0f, fadeDuration);

            _player.RemoveStatus(AgentStatus.Invincible);
            _player.PlayerInput.EnableInput();
            IsRespawning = false;

            if (!isDeath)
                ApplyFallDamage();
        }

        private static IEnumerator FadeCoroutine(float alpha, float duration)
        {
            EventBus<ScreenFadeEvent>.Raise(new ScreenFadeEvent(alpha, duration));
            yield return new WaitForSeconds(duration);
        }
        
        private void ApplyFallDamage()
        {
            _player.ApplyDamage(new DamageData(FALL_DAMAGE, _player.transform.position, Vector3.up, null, false));
        }
    }
}