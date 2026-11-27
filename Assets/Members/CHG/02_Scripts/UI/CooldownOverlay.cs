using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class CooldownOverlay : VisualElement
    {
        private static readonly CustomStyleProperty<Color> OverlayColorProperty = new ("--overlay-color");

        private Color _overlayColor = new(0f, 0f, 0f, 0.7f);
        private float _value;

        public float Value
        {
            get => _value;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_value, value))  return;
                _value = value;
                MarkDirtyRepaint();
            }
        }

        public CooldownOverlay()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += HandleGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
        }

        private void HandleGenerateVisualContent(MeshGenerationContext context)
        {
            if (_value <= 0f) return;

            Rect rect = contentRect;
            float radius = rect.size.magnitude * 0.5f;
            float startAngle = -90f + (1f - _value) * 360f;

            Painter2D painter = context.painter2D;
            painter.fillColor = _overlayColor;
            painter.BeginPath();
            painter.MoveTo(rect.center);
            painter.Arc(rect.center, radius, Angle.Degrees(startAngle), 
                Angle.Degrees(270f));
            painter.ClosePath();
            painter.Fill();
        }

        private void HandleCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(OverlayColorProperty, out Color color))
                _overlayColor = color;
            MarkDirtyRepaint();
        }
    }
}