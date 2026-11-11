using System;
using CHG._02_Scripts.Agent;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CHG._02_Scripts.Player
{
    [Serializable]
    public struct CharacterChangeSettings
    {
        public string displayName;
        public PlayerController player;
        public CinemachineCamera camera;
    }
    
    public class CharacterChange : MonoBehaviour
    {
        [SerializeField] private CharacterChangeSettings[] settings;
        [SerializeField] private int startIndex;
        [SerializeField] private int activePriority;
        [SerializeField] private int inactivePriority;
        [SerializeField] private float blendTimeout = 3f;

        private int _currentIndex = -1;
        private int _targetIndex = -1;
        private bool _isChanging;
        private float _timeoutTime;

        public int Count => settings.Length;
        public int CurrentIndex => _currentIndex;
        public bool IsChanging => _isChanging;

        private void OnEnable()
        {
             CinemachineCore.BlendFinishedEvent.AddListener(HandleBlendFinished);
        }

        private void OnDisable()
        {
            CinemachineCore.BlendFinishedEvent.RemoveListener(HandleBlendFinished);
        }

        private void Start()
        {
            for (int i = 0; i < settings.Length; i++)
                Freeze(settings[i].player);
            
            _currentIndex = startIndex;
            FocusCamera(_currentIndex); 
            Unfreeze(settings[_currentIndex].player);
            
            PlayerController current = settings[_currentIndex].player;
            EventBus<CharacterChangeStartEvent>.Raise(new CharacterChangeStartEvent(null, current));
            EventBus<CharacterChangeEndedEvent>.Raise(new CharacterChangeEndedEvent(null, current));
        }

        private void Update()
        {
            if (_isChanging && Time.time >= _timeoutTime)
            {
                Debug.LogWarning($"Bland finish timed out. Check brain blend is not Cut : {gameObject.name}");
                CompleteChange();
            }
        }

        public bool TryChangeCharacter(int index)
        {
            if (_isChanging || index == _currentIndex) return false;
            if (index < 0 || index >= settings.Length) return false;
            
            _isChanging = true;
            _targetIndex = index;
            _timeoutTime = Time.time + blendTimeout;
            
            PlayerController prev = settings[_currentIndex].player;
            PlayerController next = settings[_targetIndex].player;

            Freeze(prev);
            EventBus<CharacterChangeStartEvent>.Raise(new CharacterChangeStartEvent(prev, next));
            FocusCamera(_targetIndex);
            return true;
        }

        private void HandleBlendFinished(ICinemachineMixer mixer, ICinemachineCamera camera)
        {
            if (!_isChanging) return;
            if (!ReferenceEquals(camera, settings[_targetIndex].camera)) return;

            CompleteChange();
        }
        
        public string GetDisplayName(int index) => settings[index].displayName;

        public void LockInput() => settings[_currentIndex].player.PlayerInput.DisableInput();
        
        public void UnlockInput() => settings[_currentIndex].player.PlayerInput.EnableInput();

        private void CompleteChange()
        {
            PlayerController prev = settings[_currentIndex].player;
            PlayerController next = settings[_targetIndex].player;

            _currentIndex = _targetIndex;
            _targetIndex = -1;
            _isChanging = false;

            Unfreeze(settings[_currentIndex].player);
            EventBus<CharacterChangeEndedEvent>.Raise(new CharacterChangeEndedEvent(prev, next));
        }

        private void FocusCamera(int index)
        {
            for (int i = 0; i < settings.Length; i++)
                settings[i].camera.Priority = i == index ? activePriority : inactivePriority;
        }

        private static void Freeze(PlayerController player)
        {
            player.PlayerInput.DisableInput();
            player.AddStatus(AgentStatus.Invincible);
        }

        private static void Unfreeze(PlayerController player)
        {
            player.RemoveStatus(AgentStatus.Invincible);
            player.PlayerInput.EnableInput();
        }
        
        public PlayerController GetPlayer(int index) => settings[index].player; 
    }
}