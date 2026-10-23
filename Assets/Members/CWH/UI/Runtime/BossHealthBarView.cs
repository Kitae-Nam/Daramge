using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Daramge.BossUI
{
    /// <summary>UXML 표시만 담당한다. 보스 컴포넌트와 전투 규칙은 알지 않는다(SRP).</summary>
    public sealed class BossHealthBarView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _portrait;
        private readonly Label _fallback;
        private readonly Label _name;
        private readonly VisualElement _phases;
        private readonly VisualElement _current;
        private readonly VisualElement _lag;
        private readonly Label _percent;

        public BossHealthBarView(VisualElement root)
        {
            _root = Require<VisualElement>(root, "boss-hud");
            _portrait = Require<VisualElement>(root, "boss-portrait");
            _fallback = Require<Label>(root, "portrait-fallback");
            _name = Require<Label>(root, "boss-name");
            _phases = Require<VisualElement>(root, "boss-phases");
            _current = Require<VisualElement>(root, "health-current");
            _lag = Require<VisualElement>(root, "health-lag");
            _percent = Require<Label>(root, "health-percent");
        }

        public void SetIdentity(string bossName, Sprite portrait)
        {
            _name.text = bossName;
            _portrait.style.backgroundImage = portrait == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(portrait);
            _fallback.style.display = portrait == null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetPhases(int count, int currentPhase)
        {
            // 개수가 달라질 때만 생성해 매 프레임 UI 객체가 할당되지 않게 한다.
            if (_phases.childCount != count)
            {
                _phases.Clear();
                for (int i = 0; i < count; i++)
                {
                    var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                    dot.AddToClassList("phase-dot");
                    _phases.Add(dot);
                }
            }
            for (int i = 0; i < count; i++)
            {
                _phases[i].EnableInClassList("phase-dot--spent", i < currentPhase - 1);
                _phases[i].EnableInClassList("phase-dot--current", i == currentPhase - 1);
            }
        }

        public void SetHealth(float current, float trail)
        {
            _current.style.width = Length.Percent(current * 100f);
            _lag.style.width = Length.Percent(trail * 100f);
            // 살아 있는 보스의 체력이 반올림 때문에 0%로 보이지 않도록 올림한다.
            _percent.text = $"{Mathf.CeilToInt(current * 100f)}%";
        }

        public void SetVisible(bool visible) => _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            // UXML 계약이 깨지면 누락된 요소를 명확하게 알린다.
            return root.Q<T>(name) ?? throw new InvalidOperationException($"Boss HUD UXML에 '{name}' 요소가 필요합니다.");
        }
    }
}
