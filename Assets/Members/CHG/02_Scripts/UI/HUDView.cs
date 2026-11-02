using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player;
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
        
        [SerializeField] private VisualElementReference<VisualElement> heartContainer = new(); 
        [SerializeField] private VisualElementReference<RadialGauge> staminaGauge = new();
        
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
            
        }

        private void OnDestroy()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(HandleCameraUpdated);
            EventBus<CharacterChangeStartEvent>.Unsubscribe(HandleCharacterChangeStarted);
            
            heartContainer.UnregisterReferenceResolvedCallback(HandleHeartContainerResolved);
            heartContainer.UnregisterReferenceUnloadedCallback(HandleHeartContainerUnloaded);
            staminaGauge.UnregisterReferenceResolvedCallback(HandleStaminaGaugeResolved);
            staminaGauge.UnregisterReferenceUnloadedCallback(HandleStaminaGaugeUnloaded);

            DisposeHealthPresenter();
            DisposeStaminaPresenter();
        }

        private void HandleHeartContainerResolved(VisualElement container)
        {
            _heartContainer = null;
            DisposeHealthPresenter();
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
        
        private void HandleCameraUpdated(CinemachineBrain brain)
        {
            if (brain.OutputCamera != worldCamera) return;
            
            _staminaFollower?.Tick();
        }

        private void HandleCharacterChangeStarted(CharacterChangeStartEvent evt)
        {
            _player = evt.Current;
            _healthModule = _player.GetModule<PlayerHealthModule>();
            Debug.Assert(_healthModule != null, $"_healthModule is null : {gameObject.name}");
            _staminaModule = _player.GetModule<StaminaModule>();
            Debug.Assert(_staminaModule != null, $"_staminaModule is null : {gameObject.name}");

            RebuildHealthPresenter();
            RebuildStaminaPresenter();
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
    }
}