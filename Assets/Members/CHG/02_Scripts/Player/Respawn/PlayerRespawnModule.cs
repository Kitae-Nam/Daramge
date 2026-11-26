using System;
using System.Collections;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.Player.FSM;
using GGMLib.ModuleSystems;
using UnityEngine;

namespace CHG._02_Scripts.Player.Respawn
{
    public class PlayerRespawnModule : MonoBehaviour, IModule, IRespawnable
    {
        [SerializeField] private float deathRespawnDelay = 2f;
        [SerializeField] private float fallRespawnDelay = 0.5f;

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

            StartCoroutine(RespawnCoroutine(_safeGround.LastSafePosition, fallRespawnDelay, null, false));
        }

        private void HandleDeath()
        {
            StopAllCoroutines();
            Vector3 position = _currentArea != null ? _currentArea.RespawnPosition : _safeGround.LastSafePosition;
            StartCoroutine(RespawnCoroutine(position, deathRespawnDelay, _currentArea, true));
        }

        private IEnumerator RespawnCoroutine(Vector3 position, float delay, RespawnArea area, bool revive)
        {
            IsRespawning = true;
            _player.PlayerInput.DisableInput();
            _player.AddStatus(AgentStatus.Invincible);
            
            yield return new WaitForSeconds(delay);
            
            _warp.Warp(position);
            if (area != null)
            {
                area.NotifyRespawned();
                _currentArea = null;
            }
            if (revive)
                _player.Revive();
            _player.ChangeState(PlayerState.IDLE, 0f);
            
            _player.RemoveStatus(AgentStatus.Invincible);
            _player.PlayerInput.EnableInput();
            IsRespawning = false;
        }
    }
}