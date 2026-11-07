using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.Indicator
{
    public interface ISkillRangeIndicator
    {
        void Show();
        void Hide();
        void Pulse(float duration);
        void Flash(float duration);
        void SetSize(Vector2 size);
        void SetProgress(float progress);
    }
}