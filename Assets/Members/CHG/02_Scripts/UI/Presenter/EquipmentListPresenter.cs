using System;
using System.Collections.Generic;
using System.Linq;
using CHG._02_Scripts.Player.Equipment;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public class EquipmentListPresenter : IDisposable
    {
        private const string HiddenClass = "equipment--hidden";
        private const string RowEquippedClass = "equipment-row--equipped";

        private readonly VisualElement _content;
        private readonly VisualElement _empty;
        private readonly Label _title;
        private readonly Label _count;
        private readonly Label _description;
        private readonly ScrollView _rowContainer;
        private readonly VisualTreeAsset _rowTemplate;
        private readonly IEquipmentModule _model;
        private readonly IReadOnlyList<EquipmentSO> _allEquipment;
        private readonly List<Button> _rows = new();

        public EquipmentListPresenter(VisualElement root, VisualTreeAsset rowTemplate, IEquipmentModule model,
            IReadOnlyList<EquipmentSO> allEquipment)
        {
            _content = root.Q<VisualElement>("list-content");
            _empty = root.Q<VisualElement>("list-empty");
            _title = root.Q<Label>("list-title");
            _count = root.Q<Label>("list-count");
            _description = root.Q<Label>("list-description");
            _rowContainer = root.Q<ScrollView>("list-rows");
            _rowTemplate = rowTemplate;
            _model = model;
            _allEquipment = allEquipment;
            _model.OnEquipmentChanged += HandleEquipmentChanged;
        }

        public void Show(EquipmentPart? part)
        {
            ClearRows();
            _content.EnableInClassList(HiddenClass, part == null);
            _empty.EnableInClassList(HiddenClass, part != null);
            if (part == null) return;

            foreach (EquipmentSO equipment in _allEquipment.Where(item => item != null && item.Part == part))
                _rows.Add(CreateRow(equipment));

            string partName = EquipmentUIUtility.GetPartName(part.Value);
            _title.text = $"{partName} 장비";
            _count.text = $"{_rows.Count}개";
            _description.text = $"{partName} 칸에 끼울 수 있는 장비만 표시됩니다";
            RefreshRows();
        }

        private Button CreateRow(EquipmentSO equipment)
        {
            Button row = _rowTemplate.Instantiate().Q<Button>("row");
            row.userData = equipment;
            row.RegisterCallback<ClickEvent>(HandleRowClicked);

            EquipmentUIUtility.SetIcon(row.Q<VisualElement>("row-icon"), equipment.Icon);
            row.Q<Label>("row-name").text = equipment.DisplayName;
            row.Q<Label>("row-stats").text = EquipmentUIUtility.FormatBonuses(equipment.Bonuses);
            row.Q<Label>("row-sets").text = FormatSetNames(equipment);

            _rowContainer.Add(row);
            return row;
        }

        private static string FormatSetNames(EquipmentSO equipment)
            => string.Join("  ", equipment.Set.Where(set => set != null).Distinct()
                .Select(set => $"<color=#{ColorUtility.ToHtmlStringRGB(set.Color)}>{set.DisplayName}</color>"));

        private void HandleEquipmentChanged(EquipmentPart part) => RefreshRows();

        private void RefreshRows()
        {
            foreach (Button row in _rows)
            {
                EquipmentSO equipment = (EquipmentSO)row.userData;
                bool isEquipped = _model.GetEquipment(equipment.Part) == equipment;
                row.EnableInClassList(RowEquippedClass, isEquipped);
                row.Q<Label>("row-state").text = isEquipped ? "장착 중" : "장착";
            }
        }

        private void HandleRowClicked(ClickEvent evt)
        {
            EquipmentSO equipment = (EquipmentSO)((VisualElement)evt.currentTarget).userData;

            if (_model.GetEquipment(equipment.Part) == equipment)
                _model.Unequip(equipment.Part);
            else
                _model.Equip(equipment);
        }

        private void ClearRows()
        {
            foreach (Button row in _rows)
            {
                row.UnregisterCallback<ClickEvent>(HandleRowClicked);
                row.RemoveFromHierarchy();
            }

            _rows.Clear();
        }

        public void Dispose()
        {
            _model.OnEquipmentChanged -= HandleEquipmentChanged;
            ClearRows();
        }
    }
}