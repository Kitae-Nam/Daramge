using System.Collections.Generic;
using CHG._02_Scripts.CoreSystem.EventBusSystem;
using CHG._02_Scripts.CoreSystem.TimeSystem;
using CHG._02_Scripts.Player;
using CHG._02_Scripts.Player.Equipment;
using CHG._02_Scripts.UI.Presenter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class EquipmentWindowView : MonoBehaviour
    {
        private const string HiddenClass = "equipment--hidden";
        private const string TabClass = "equipment__tab";
        private const string TabSelectedClass = "equipment__tab--selected";
        private const float PausedTimeScale = 0f;

        [SerializeField] private CharacterChange characterChange;
        [SerializeField] private CharacterPreview characterPreview;
        [SerializeField] private InputActionReference cancelAction;
        [SerializeField] private EquipmentSO[] allEquipment;
        [SerializeField] private VisualElementReference<VisualElement> equipmentWindow = new();
        [SerializeField] private VisualTreeAsset rowTemplate;
        [SerializeField] private VisualTreeAsset setCardTemplate;
        [SerializeField] private VisualTreeAsset setTierTemplate;

        private readonly List<Button> _tabs = new();
        private VisualElement _window;
        private VisualElement _tabContainer;
        private EquipmentSlotsPresenter _slotsPresenter;
        private EquipmentListPresenter _listPresenter;
        private EquipmentSetPresenter _setPresenter;
        private EquipmentPart? _selectedPart;
        private int _characterIndex;
        private bool _isOpen;

        private void Awake()
        {
            Debug.Assert(characterChange != null, $"characterChange is null : {gameObject.name}");
            Debug.Assert(characterPreview != null, $"characterPreview is null : {gameObject.name}");
            EventBus<EquipmentWindowRequestEvent>.Subscribe(HandleOpenRequest);
            cancelAction.action.performed += HandleCancelPerformed;
        }

        private void Start()
        {
            equipmentWindow.RegisterReferenceResolvedCallback(HandleWindowResolved);
            equipmentWindow.RegisterReferenceUnloadedCallback(HandleWindowUnloaded);
        }

        private void OnDestroy()
        {
            EventBus<EquipmentWindowRequestEvent>.Unsubscribe(HandleOpenRequest);
            cancelAction.action.performed -= HandleCancelPerformed;
            equipmentWindow.UnregisterReferenceResolvedCallback(HandleWindowResolved);
            equipmentWindow.UnregisterReferenceUnloadedCallback(HandleWindowUnloaded);
            ReleaseWindow();
        }

        private void HandleWindowResolved(VisualElement window)
        {
            ReleaseWindow();
            _window = window;
            _tabContainer = window.Q<VisualElement>("character-tabs");
            window.Q<VisualElement>("character-preview").style.backgroundImage =
                new StyleBackground(Background.FromRenderTexture(characterPreview.Texture));

            BuildTabs();
            _window.AddToClassList(HiddenClass);
        }

        private void HandleWindowUnloaded(VisualElement window) => ReleaseWindow();

        private void BuildTabs()
        {
            for (int i = 0; i < characterChange.Count; i++)
            {
                Button tab = new() { text = characterChange.GetDisplayName(i), userData = i };
                tab.AddToClassList(TabClass);
                tab.RegisterCallback<ClickEvent>(HandleTabClicked);
                _tabContainer.Add(tab);
                _tabs.Add(tab);
            }
        }

        private void ReleaseWindow()
        {
            Close();

            foreach (Button tab in _tabs)
            {
                tab.UnregisterCallback<ClickEvent>(HandleTabClicked);
                tab.RemoveFromHierarchy();
            }

            _tabs.Clear();
            _window = null;
            _tabContainer = null;
        }

        private void HandleOpenRequest(EquipmentWindowRequestEvent evt) => Open();

        private void Open()
        {
            if (_isOpen || _window == null || characterChange.IsChanging) return;

            _isOpen = true;
            _selectedPart = null;
            characterChange.LockInput();
            EventBus<TimeScaleRequestEvent>.Raise(new TimeScaleRequestEvent(this, PausedTimeScale));
            ShowCharacter(characterChange.CurrentIndex);
            _window.RemoveFromClassList(HiddenClass);
        }

        private void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            DisposePresenters();
            characterPreview.Hide();
            _window?.AddToClassList(HiddenClass);
            EventBus<TimeScaleReleaseEvent>.Raise(new TimeScaleReleaseEvent(this));
            characterChange.UnlockInput();
        }

        private void ShowCharacter(int index)
        {
            DisposePresenters();
            _characterIndex = index;

            PlayerController player = characterChange.GetPlayer(index);
            IEquipmentModule equipmentModule = player.GetModule<IEquipmentModule>();
            EquipmentSetBonusModule setModule = player.GetModule<EquipmentSetBonusModule>();
            Debug.Assert(equipmentModule != null, $"equipmentModule is null : {player.name}");
            Debug.Assert(setModule != null, $"setModule is null : {player.name}");

            _slotsPresenter = new EquipmentSlotsPresenter(_window, equipmentModule);
            _slotsPresenter.OnSlotClicked += HandleSlotClicked;
            _listPresenter = new EquipmentListPresenter(_window, rowTemplate, equipmentModule, allEquipment);
            _setPresenter = new EquipmentSetPresenter(_window, setCardTemplate, setTierTemplate, setModule);

            ApplySelectedPart();
            RefreshTabs();
            characterPreview.Show(index);
        }

        private void ApplySelectedPart()
        {
            _slotsPresenter.SetSelected(_selectedPart);
            _listPresenter.Show(_selectedPart);
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
                _tabs[i].EnableInClassList(TabSelectedClass, i == _characterIndex);
        }

        private void HandleSlotClicked(EquipmentPart part)
        {
            _selectedPart = _selectedPart == part ? null : part;
            ApplySelectedPart();
        }

        private void HandleTabClicked(ClickEvent evt)
        {
            int index = (int)((VisualElement)evt.currentTarget).userData;
            if (_isOpen && index != _characterIndex)
                ShowCharacter(index);
        }

        private void HandleCancelPerformed(InputAction.CallbackContext context) => Close();

        private void DisposePresenters()
        {
            if (_slotsPresenter != null)
                _slotsPresenter.OnSlotClicked -= HandleSlotClicked;

            _slotsPresenter?.Dispose();
            _listPresenter?.Dispose();
            _setPresenter?.Dispose();
            _slotsPresenter = null;
            _listPresenter = null;
            _setPresenter = null;
        }
    }
}