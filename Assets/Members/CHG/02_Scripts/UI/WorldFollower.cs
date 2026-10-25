using UnityEngine;
using UnityEngine.UIElements;

namespace CHG._02_Scripts.UI
{
    public class WorldFollower
    {
        private readonly VisualElement _element;
        private readonly Transform _target;
        private readonly Camera _camera;
        private readonly Vector2 _panelOffset;

        public WorldFollower(VisualElement element, Transform target, Camera camera, Vector2 panelOffset)
        {
            _element = element;
            _target = target;
            _camera = camera;
            _panelOffset = panelOffset;
        }

        public void Tick()
        {
            if (_element.panel == null || _target == null) return;
            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(_element.panel, _target.position, _camera);
            float x = panelPosition.x + _panelOffset.x - _element.resolvedStyle.width * 0.5f;
            float y = panelPosition.y + _panelOffset.y - _element.resolvedStyle.height * 0.5f;
            _element.style.translate = new Translate(x, y);
        }
    }
}