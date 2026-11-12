using System;
using System.Collections.Generic;
using System.Linq;
using CHG._02_Scripts.Player.Equipment;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public class EquipmentSetPresenter : IDisposable
    {
        private const string HiddenClass = "equipment--hidden";
        private const string PipClass = "equipment-set__pip";
        private const string TierLockedClass = "equipment-tier--locked";

        private readonly ScrollView _cardContainer;
        private readonly Label _count;
        private readonly VisualElement _empty;
        private readonly VisualTreeAsset _cardTemplate;
        private readonly VisualTreeAsset _tierTemplate;
        private readonly EquipmentSetBonusModule _model;

        public EquipmentSetPresenter(VisualElement root, VisualTreeAsset cardTemplate, VisualTreeAsset tierTemplate,
            EquipmentSetBonusModule model)
        {
            _cardContainer = root.Q<ScrollView>("set-cards");
            _count = root.Q<Label>("set-count");
            _empty = root.Q<VisualElement>("set-empty");
            _cardTemplate = cardTemplate;
            _tierTemplate = tierTemplate;
            _model = model;
            _model.OnSetCountChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            _cardContainer.Clear();
            int cardCount = 0;

            foreach (KeyValuePair<EquipmentSetSO, int> setPair in _model.CountBySet.OrderBy(pair => pair.Key.DisplayName))
            {
                int reachedTier = setPair.Key.Tiers.Count(_model.IsTierActive);
                if (reachedTier == 0) continue;

                CreateCard(setPair.Key, setPair.Value, reachedTier);
                cardCount++;
            }

            _count.text = $"{cardCount}개";
            _empty.EnableInClassList(HiddenClass, cardCount > 0);
        }

        private void CreateCard(EquipmentSetSO set, int count, int reachedTier)
        {
            VisualElement card = _cardTemplate.Instantiate().Q<VisualElement>("set-card");
            card.Q<Label>("set-name").text = set.DisplayName;

            Label badge = card.Q<Label>("set-badge");
            badge.text = $"티어 {reachedTier}";
            badge.style.backgroundColor = set.Color;

            int maxCount = set.Tiers.Max(tier => tier.RequiredCount);
            VisualElement pips = card.Q<VisualElement>("set-pips");

            for (int i = 0; i < maxCount; i++)
            {
                VisualElement pip = new();
                pip.AddToClassList(PipClass);
                if (i < count)
                    pip.style.backgroundColor = set.Color;
                pips.Add(pip);
            }

            card.Q<Label>("set-progress-text").text = $"{Mathf.Min(count, maxCount)} / {maxCount}";

            VisualElement tiers = card.Q<VisualElement>("set-tiers");
            foreach (EquipmentSetTier tier in set.Tiers.OrderBy(item => item.RequiredCount))
                tiers.Add(CreateTierRow(tier, count));

            _cardContainer.Add(card);
        }

        private VisualElement CreateTierRow(EquipmentSetTier tier, int count)
        {
            bool isReached = _model.IsTierActive(tier);

            VisualElement row = _tierTemplate.Instantiate().Q<VisualElement>("tier");
            row.EnableInClassList(TierLockedClass, !isReached);
            row.Q<Label>("tier-need").text = $"{tier.RequiredCount}개";
            row.Q<Label>("tier-effect").text = EquipmentUIUtility.FormatBonuses(tier.Bonuses);
            row.Q<Label>("tier-note").text = isReached ? string.Empty : $"{tier.RequiredCount - count}개 더";
            return row;
        }

        public void Dispose()
        {
            _model.OnSetCountChanged -= Refresh;
            _cardContainer.Clear();
        }
    }
}