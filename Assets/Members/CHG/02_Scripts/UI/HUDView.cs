using CHG._02_Scripts.Player;
using CHG._02_Scripts.UI.Presenter;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector2 staminaGaugeOffset;
        
        [SerializeField] private VisualElementReference<VisualElement> heartContainer = new(); 
        [SerializeField] private VisualElementReference<RadialGauge> staminaGauge = new();
        private VisualElement _root;
        private int _uiVersion = -1;
        private bool _isStarted;

        private PlayerHealthModule _healthModule;
        private StaminaModule _staminaModule;
        private HealthPresenter _healthPresenter;
        private StaminaPresenter _staminaPresenter;
        private WorldFollower _staminaFollower;

        private void Awake()
        {
            Debug.Assert(player != null, $"player is null : {gameObject.name}");
            Debug.Assert(worldCamera != null, $"worldCamera is null : {gameObject.name}");
            CinemachineCore.CameraUpdatedEvent.AddListener(HandleCameraUpdated);
        }

        private void Start()
        {
            _healthModule = player.GetModule<PlayerHealthModule>();
            Debug.Assert(_healthModule != null, $"healthModule is null : {gameObject.name}");
            _staminaModule = player.GetModule<StaminaModule>();
            Debug.Assert(_staminaModule != null, $"staminaModule is null : {gameObject.name}");
            
            heartContainer.RegisterReferenceResolvedCallback(HandleHeartContainerResolved);
            heartContainer.RegisterReferenceUnloadedCallback(HandleHeartContainerUnloaded);
            staminaGauge.RegisterReferenceResolvedCallback(HandleStaminaGaugeResolved);
            staminaGauge.RegisterReferenceUnloadedCallback(HandleStaminaGaugeUnloaded);
            
        }

        private void OnDestroy()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(HandleCameraUpdated);
            
            heartContainer.UnregisterReferenceResolvedCallback(HandleHeartContainerResolved);
            heartContainer.UnregisterReferenceUnloadedCallback(HandleHeartContainerUnloaded);
            staminaGauge.UnregisterReferenceResolvedCallback(HandleStaminaGaugeResolved);
            staminaGauge.UnregisterReferenceUnloadedCallback(HandleStaminaGaugeUnloaded);

            DisposeHealthPresenter();
            DisposeStaminaPresenter();
        }

        private void HandleHeartContainerResolved(VisualElement container)
        {
            DisposeHealthPresenter();
            _healthPresenter = new HealthPresenter(container, _healthModule);
        }

        private void HandleHeartContainerUnloaded(VisualElement container)
            => DisposeHealthPresenter();
        
        private void HandleStaminaGaugeResolved(RadialGauge gauge)
        {
            DisposeStaminaPresenter();
            _staminaPresenter = new StaminaPresenter(gauge, _staminaModule);
            _staminaFollower = new WorldFollower(gauge, player.transform, worldCamera, staminaGaugeOffset);
        }

        private void HandleStaminaGaugeUnloaded(RadialGauge gauge)
            => DisposeStaminaPresenter();
            
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
    }
}