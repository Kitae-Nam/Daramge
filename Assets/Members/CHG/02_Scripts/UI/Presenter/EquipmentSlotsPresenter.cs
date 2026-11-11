using System;
using System.Collections.Generic;
using CHG._02_Scripts.Player.Equipment;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public class EquipmentSlotsPresenter : IDisposable
    {
        private const string SelectedClass = "equipment-slot--selected";
        private const string EmptyClass = "equipment-slot--empty";
        private const string EmptyText = "비어 있음";

        private readonly IEquipmentModule _model;
        private readonly Dictionary<EquipmentPart, Button> _slots = new();

        public event Action<EquipmentPart> OnSlotClicked;

        public EquipmentSlotsPresenter(VisualElement root, IEquipmentModule model)
        {
            _model = model;

            foreach (EquipmentPart part in Enum.GetValues(typeof(EquipmentPart)))
            {
                Button slot = root.Q<Button>($"slot-{part}");
                slot.userData = part;
                slot.RegisterCallback<ClickEvent>(HandleSlotClicked);
                _slots.Add(part, slot);
                Refresh(part);
            }

            _model.OnEquipmentChanged += Refresh;
        }

        public void SetSelected(EquipmentPart? selectedPart)
        {
            foreach (KeyValuePair<EquipmentPart, Button> slotPair in _slots)
                slotPair.Value.EnableInClassList(SelectedClass, slotPair.Key == selectedPart);
        }

        private void Refresh(EquipmentPart part)
        {
            Button slot = _slots[part];
            EquipmentSO equipment = _model.GetEquipment(part);

            slot.EnableInClassList(EmptyClass, equipment == null);
            slot.Q<Label>("slot-item").text = equipment != null ? equipment.DisplayName : EmptyText;
            EquipmentUIUtility.SetIcon(slot.Q<VisualElement>("slot-icon"), equipment != null ? equipment.Icon : null);
        }

        private void HandleSlotClicked(ClickEvent evt)
            => OnSlotClicked?.Invoke((EquipmentPart)((VisualElement)evt.currentTarget).userData);

        public void Dispose()
        {
            _model.OnEquipmentChanged -= Refresh;

            foreach (Button slot in _slots.Values)
                slot.UnregisterCallback<ClickEvent>(HandleSlotClicked);

            _slots.Clear();
        }
    }
}