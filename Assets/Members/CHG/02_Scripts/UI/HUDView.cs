using System;
using CHG._02_Scripts.CombatSystem;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player;
using CHG._02_Scripts.Player.Combat;
using CHG._02_Scripts.UI.Presenter;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector2 staminaGaugeOffset;
        [SerializeField] private PlayerSkillSlot[] displaySkillSlots = {PlayerSkillSlot.FirstSkill, PlayerSkillSlot.SecondSkill};
        
        [SerializeField] private VisualElementReference<VisualElement> heartContainer = new(); 
        [SerializeField] private VisualElementReference<VisualElement> skillContainer = new();
        [SerializeField] private VisualElementReference<RadialGauge> staminaGauge = new();
        [SerializeField] private VisualElementReference<VisualElement> letterbox = new();
        [SerializeField] private VisualElementReference<VisualElement> screenFade = new();
        
        private VisualElement _root;
        private int _uiVersion = -1;
        private bool _isStarted;

        private PlayerController _player;
        private VisualElement _heartContainer;
        private RadialGauge _staminaGauge;
        private PlayerHealthModule _healthModule;
        private StaminaModule _staminaModule;
        private HealthPresenter _healthPresenter;
        private StaminaPresenter _staminaPresenter;
        private WorldFollower _staminaFollower;
        private LetterboxPresenter _letterboxPresenter;
        private VisualElement _skillContainer;
        private ISkillProvider _skillProvider;
        private SkillBarPresenter _skillBarPresenter;
        private ScreenFadePresenter _screenFadePresenter;

        private void Awake()
        {
            Debug.Assert(worldCamera != null, $"worldCamera is null : {gameObject.name}");
            CinemachineCore.CameraUpdatedEvent.AddListener(HandleCameraUpdated);
            EventBus<CharacterChangeStartEvent>.Subscribe(HandleCharacterChangeStarted);
        }

        private void Start()
        {
            heartContainer.RegisterReferenceResolvedCallback(HandleHeartContainerResolved);
            heartContainer.RegisterReferenceUnloadedCallback(HandleHeartContainerUnloaded);
            staminaGauge.RegisterReferenceResolvedCallback(HandleStaminaGaugeResolved);
            staminaGauge.RegisterReferenceUnloadedCallback(HandleStaminaGaugeUnloaded);
            letterbox.RegisterReferenceResolvedCallback(HandleLetterboxResolved);
            letterbox.RegisterReferenceUnloadedCallback(HandleLetterboxUnloaded);
            skillContainer.RegisterReferenceResolvedCallback(HandleSkillContainerResolved);
            skillContainer.RegisterReferenceUnloadedCallback(HandleSkillContainerUnloaded);
            screenFade.RegisterReferenceResolvedCallback(HandleScreenFadeResolved);
            screenFade.RegisterReferenceUnloadedCallback(HandleScreenFadeUnloaded);
        }

        private void Update() => _skillBarPresenter?.Tick();

        private void OnDestroy()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(HandleCameraUpdated);
            EventBus<CharacterChangeStartEvent>.Unsubscribe(HandleCharacterChangeStarted);
    
            heartContainer.UnregisterReferenceResolvedCallback(HandleHeartContainerResolved);
            heartContainer.UnregisterReferenceUnloadedCallback(HandleHeartContainerUnloaded);
            staminaGauge.UnregisterReferenceResolvedCallback(HandleStaminaGaugeResolved);
            staminaGauge.UnregisterReferenceUnloadedCallback(HandleStaminaGaugeUnloaded);
            letterbox.UnregisterReferenceResolvedCallback(HandleLetterboxResolved); 
            letterbox.UnregisterReferenceUnloadedCallback(HandleLetterboxUnloaded); 
            screenFade.UnregisterReferenceResolvedCallback(HandleScreenFadeResolved);
            screenFade.UnregisterReferenceUnloadedCallback(HandleScreenFadeUnloaded);

            DisposeHealthPresenter();
            DisposeStaminaPresenter();
            DisposeLetterboxPresenter(); 
            DisposeSkillBarPresenter();
            DisposeScreenFadePresenter();
        }

        private void HandleHeartContainerResolved(VisualElement container)
        {
            _heartContainer = container;
            RebuildHealthPresenter();
        }

        private void HandleHeartContainerUnloaded(VisualElement container)
        {
            _heartContainer = null;
            DisposeHealthPresenter();
        }

        private void HandleStaminaGaugeResolved(RadialGauge gauge)
        {
            _staminaGauge = gauge;
            RebuildStaminaPresenter();
        }

        private void HandleStaminaGaugeUnloaded(RadialGauge gauge)
        {
            _staminaGauge = null;
            DisposeStaminaPresenter();
        }
            
        private void DisposeHealthPresenter()
        {
            _healthPresenter?.Dispose();
            _healthPresenter = null;
        }

        private void DisposeStaminaPresenter()
        {
            _staminaPresenter?.Dispose();
            _staminaPresenter = null;
            _staminaFollower = null;
        }
        
        private void DisposeSkillBarPresenter()
        {
            _skillBarPresenter?.Dispose();
            _skillBarPresenter = null;
        }

        private void HandleCameraUpdated(CinemachineBrain brain)
        {
            if (brain.OutputCamera != worldCamera) return;
            
            _staminaFollower?.Tick();
        }

        private void HandleCharacterChangeStarted(CharacterChangeStartEvent evt)
        {
            _player = evt.Current;
            _healthModule = _player.GetModule<PlayerHealthModule>();
            _staminaModule = _player.GetModule<StaminaModule>();
            _skillProvider = _player.GetModule<ISkillProvider>();
            Debug.Assert(_healthModule != null, $"healthModule is null : {gameObject.name}");
            Debug.Assert(_staminaModule != null, $"staminaModule is null : {gameObject.name}");
            Debug.Assert(_skillProvider != null, $"skillProvider is null : {gameObject.name}");
            

            RebuildHealthPresenter();
            RebuildStaminaPresenter();
            RebuildSkillBarPresenter();
        }

        private void RebuildHealthPresenter()
        {
            DisposeHealthPresenter();
            if (_heartContainer == null || _healthModule == null) return;
            _healthPresenter = new HealthPresenter(_heartContainer, _healthModule);
        }

        private void RebuildStaminaPresenter()
        {
            DisposeStaminaPresenter();
            if (_staminaGauge == null || _staminaModule == null) return;
            _staminaPresenter = new StaminaPresenter(_staminaGauge, _staminaModule);
            _staminaFollower = new WorldFollower(_staminaGauge, _player.transform, worldCamera, staminaGaugeOffset);
        }

        private void RebuildSkillBarPresenter()
        {
            DisposeSkillBarPresenter();
            if (_skillContainer == null || _skillProvider == null) return;
            _skillBarPresenter = new SkillBarPresenter(_skillContainer, _skillProvider, _player.PlayerInput, displaySkillSlots);
        }

        

        private void HandleLetterboxResolved(VisualElement element) 
        {
            DisposeLetterboxPresenter();
            _letterboxPresenter = new LetterboxPresenter(element);
        }

        private void HandleLetterboxUnloaded(VisualElement element) => DisposeLetterboxPresenter();

        private void HandleSkillContainerResolved(VisualElement container)
        {
            _skillContainer = container;
            RebuildSkillBarPresenter();
        }

        private void HandleSkillContainerUnloaded(VisualElement container)
        {
            _skillBarPresenter = null;
            DisposeSkillBarPresenter();
        }

        private void DisposeLetterboxPresenter() 
        {
            _letterboxPresenter?.Dispose();
            _letterboxPresenter = null;
        }
        
        private void HandleScreenFadeResolved(VisualElement element)
        {
            DisposeScreenFadePresenter();
            _screenFadePresenter = new ScreenFadePresenter(element);
        }
        
        private void HandleScreenFadeUnloaded(VisualElement element) => DisposeScreenFadePresenter();
        
        private void DisposeScreenFadePresenter()
        {
            _screenFadePresenter?.Dispose();
            _screenFadePresenter = null;
        }
    }
}