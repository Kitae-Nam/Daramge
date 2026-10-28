using System;
using CHG._02_Scripts.Player;
using CHG._02_Scripts.UI.Presenter;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class HUDView : MonoBehaviour
    {
        [SerializeField] private PanelRenderer panelRenderer;
        [SerializeField] private PlayerController player;
        
        private VisualElement _root;
        private int _uiVersion = -1;
        private bool _isStarted;

        private HealthPresenter _healthPresenter;
        private StaminaPresenter _staminaPresenter;

        private void Awake()
        {
            Debug.Assert(panelRenderer != null, $"panelRenderer is null : {gameObject.name}");
            Debug.Assert(player != null, $"playerController is null : {gameObject.name}");
            
            panelRenderer.RegisterUIReloadCallback(HandleUiReload);
        }

        private void Start()
        {
            _isStarted = true;
            BindPresenters();
        }

        private void OnDestroy()
        {
            panelRenderer.UnregisterUIReloadCallback(HandleUiReload);
            DisposePresenters();
        }

        private void HandleUiReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (_uiVersion == version) return;
            
            _uiVersion = version;
            _root = root;
            BindPresenters();
        }

        private void BindPresenters()
        {
            if (!_isStarted || _root == null) return;

            DisposePresenters();
            
            PlayerHealthModule healthModule = player.GetModule<PlayerHealthModule>();
            Debug.Assert(healthModule != null, $"healthModule is null : {gameObject.name}");
            
            _healthPresenter = new HealthPresenter(_root.Q<VisualElement>("heart-container"), healthModule);
            
            StaminaModule staminaModule = player.GetModule<StaminaModule>();
            Debug.Assert(staminaModule != null, $"staminaModule is null : {gameObject.name}");
            
            RadialGauge staminaGauge = _root.Q<RadialGauge>("stamina-gauge");
            Debug.Assert(staminaGauge != null, $"staminaGauge is null : {gameObject.name}");
            
            _staminaPresenter = new StaminaPresenter(staminaGauge, staminaModule);
        }

        private void DisposePresenters()
        {
            _healthPresenter?.Dispose();
            _healthPresenter = null;
            
            _staminaPresenter?.Dispose();
            _staminaPresenter = null;
        }
    }
}