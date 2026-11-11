using System.Collections.Generic;
using System.Linq;
using CHG._02_Scripts.Agent.StatSystem;
using CHG._02_Scripts.Player.Equipment;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public static class EquipmentUIUtility
    {
        public static string GetPartName(EquipmentPart part) => part switch
        {
            EquipmentPart.LeftArm => "왼팔",
            EquipmentPart.RightArm => "오른팔",
            EquipmentPart.LeftLeg => "왼다리",
            _ => "오른다리"
        };

        public static string FormatBonuses(IEnumerable<StatBonus> bonuses)
            => string.Join(" · ", bonuses.Where(bonus => bonus.StatData != null).Select(FormatBonus));

        private static string FormatBonus(StatBonus bonus)
        {
            bool isPercent = bonus.Type == StatModifierType.PercentAdd || bonus.StatData.IsPercent;
            float value = isPercent ? bonus.Value * 100f : bonus.Value;
            return $"{bonus.StatData.StatName} {value:+0.##;-0.##}{(isPercent ? "%" : "")}";
        }

        public static void SetIcon(VisualElement icon, Sprite sprite)
            => icon.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
    }
}