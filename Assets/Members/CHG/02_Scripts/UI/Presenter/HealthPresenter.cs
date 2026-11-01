using System;
using System.Collections.Generic;
using CHG._02_Scripts.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI.Presenter
{
    public class HealthPresenter : IDisposable
    {
        private const int HpPerHeart = 2;
        private const string HeartClass = "heart";
        private const string FillClass = "heart__fill";
        private const string FillImageClass = "heart__fill-image";

        private readonly VisualElement _container;
        private readonly PlayerHealthModule _model;
        private readonly List<VisualElement> _fills = new();

        public HealthPresenter(VisualElement container, PlayerHealthModule model)
        {
            _container = container;
            _model = model;
            _model.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_model.CurrentHealth, _model.MaxHealth);
        }

        private void HandleHealthChanged(int current, int max)
        {
            EnsureHeartCount(Mathf.CeilToInt(max / (float)HpPerHeart));

            for (int i = 0; i < _fills.Count; i++)
            {
                int amount = Mathf.Clamp(current - i * HpPerHeart, 0, HpPerHeart);
                _fills[i].style.width = Length.Percent(amount * 100f / HpPerHeart);
            }
        }

        private void EnsureHeartCount(int count)
        {
            while (_fills.Count < count)
            {
                VisualElement heart = new();
                heart.AddToClassList(HeartClass);
                
                VisualElement fill = new();
                fill.AddToClassList(FillClass);
                
                VisualElement fillImage = new();
                fillImage.AddToClassList(FillImageClass);
                
                fill.Add(fillImage);
                heart.Add(fill);
                _container.Add(heart);
                _fills.Add(fill);
            }

            while (_fills.Count > count)
            {
                int last = _fills.Count - 1;
                _fills[last].parent.RemoveFromHierarchy();
                _fills.RemoveAt(last);
            }
        }

        public void Dispose()
            => _model.OnHealthChanged -= HandleHealthChanged;
    }
}