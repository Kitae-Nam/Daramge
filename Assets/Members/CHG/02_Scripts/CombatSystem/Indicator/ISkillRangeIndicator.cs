using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.Indicator
{
    public interface ISkillRangeIndicator
    {
        void Show();
        void Hide();
        void SetSize(Vector2 size);
        void SetProgress(float progress);
    }
}