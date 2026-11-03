using System;
using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class CharacterSelectView : MonoBehaviour
    {
        private const string HiddenClass = "character-select--hidden";
        private const string ButtonClass = "character-select__button";
        private const string ButtonContainerName = "character-button-container";
        private const string CancelButtonName = "cancel-button";
        
        [SerializeField] private CharacterChange characterChange;
        [SerializeField] private InputActionReference cancelAction;
        [SerializeField] private VisualElementReference<VisualElement> selectPanel = new();
        
        private readonly List<Button> _characterButtons = new();
        private VisualElement _panel;
        private VisualElement _buttonContainer;
        private Button _cancelButton;
        private bool _isOpen;

        private void Awake()
        {
            Debug.Assert(characterChange != null, $"characterChange is null : {gameObject.name}" );
            EventBus<CharacterSelectedRequestEvent>.Subscribe(HandleSelectRequest);
            cancelAction.action.performed += HandleCancelPerformed;
        }

        private void Start()
        {
            selectPanel.RegisterReferenceResolvedCallback(HandleSelectPanelResolved);
            selectPanel.RegisterReferenceUnloadedCallback(HandleSelectPanelUnloaded);
        }

        private void OnDestroy()
        {
            EventBus<CharacterSelectedRequestEvent>.Unsubscribe(HandleSelectRequest);
            cancelAction.action.performed -= HandleCancelPerformed;
            selectPanel.UnregisterReferenceResolvedCallback(HandleSelectPanelResolved);
            selectPanel.UnregisterReferenceUnloadedCallback(HandleSelectPanelUnloaded);
            ReleasePanel();
        }

        private void HandleSelectPanelResolved(VisualElement panel)
        {
            ReleasePanel();
            _panel = panel;
            _buttonContainer = panel.Q<VisualElement>(ButtonContainerName);
            _cancelButton = panel.Q<Button>(CancelButtonName);

            _cancelButton.clicked += HandleCancelClicked;
            BuildCharacterButtons();
            RefreshButtons();
            _panel.EnableInClassList(HiddenClass, !_isOpen);
        }

        private void HandleSelectPanelUnloaded(VisualElement panel)
            => ReleasePanel();

        private void BuildCharacterButtons()
        {
            for (int i = 0; i < characterChange.Count; i++)
            {
                Button btn = new() { text = characterChange.GetDisplayName(i), userData = i };
                btn.AddToClassList(ButtonClass);
                btn.RegisterCallback<ClickEvent>(HandleCharacterButtonClicked);
                _buttonContainer.Add(btn);
                _characterButtons.Add(btn);
            }
        }

        private void ReleasePanel()
        {
            foreach (Button btn in _characterButtons)
            {
                btn.UnregisterCallback<ClickEvent>(HandleCharacterButtonClicked);
                btn.RemoveFromHierarchy();
            }
            
            _characterButtons.Clear();

            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancelClicked;
            
            _panel = null;
            _buttonContainer = null;
            _cancelButton = null;
        }

        private void HandleSelectRequest(CharacterSelectedRequestEvent evt) => Open();

        private void Open()
        {
            if (_isOpen || _panel == null || characterChange.IsChanging) return;

            _isOpen = true;
            characterChange.LockInput();
            RefreshButtons();
            _panel.RemoveFromClassList(HiddenClass);
        }

        private void Close()
        {
            _isOpen = false;
            _panel?.AddToClassList(HiddenClass);
        }

        private void RefreshButtons()
        {
            for (int i = 0; i < _characterButtons.Count; i++)
                _characterButtons[i].SetEnabled(i != characterChange.CurrentIndex);
        }

        private void HandleCharacterButtonClicked(ClickEvent evt)
        {
            if (!_isOpen) return;

            int index = (int)((VisualElement)evt.currentTarget).userData;
            Close();
            if (!characterChange.TryChangeCharacter(index))
                characterChange.UnlockInput();
        }

        private void HandleCancelClicked() => Cancel();

        private void HandleCancelPerformed(InputAction.CallbackContext context) => Cancel();

        private void Cancel()
        {
            if (!_isOpen) return;
            
            Close();
            characterChange.UnlockInput();
        }
    }
}