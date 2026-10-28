using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    [UxmlElement]
    public partial class RadialGauge : VisualElement
    {
        private float _value = 1f;

        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute] public float Thickness { get; set; } = 10f;
        [UxmlAttribute] public Color FillColor { get; set; } = new Color(0.1f, 0.95f, 0.45f);
        [UxmlAttribute] public Color TrackColor { get; set; } = new Color(0.3f, 0.18f, 0.12f);

        public RadialGauge()
        {
            generateVisualContent += HandleGenerateVisualContent;
        }

        private void HandleGenerateVisualContent(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Rect rect = contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - Thickness * 0.5f;
            if (radius <= 0f) return;

            painter.lineWidth = Thickness;
            painter.lineCap = LineCap.Butt;
            
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            if (_value <= 0f) return;

            float startAngle = -90f + (1f - _value) * 360f;
            painter.strokeColor = FillColor;
            painter.BeginPath();
            painter.Arc(rect.center, radius, Angle.Degrees(startAngle), Angle.Degrees(270f));
            painter.Stroke();
        }
    }
}