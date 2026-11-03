using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.Indicator
{
    public class RectangleRangeIndicator : AbstractQuadRangeIndicator
    {
        private const string WIDTH_PROPERTY = "_OutRectangleW";
        private const string HEIGHT_PROPERTY = "_OutRectangleH";
        
        protected override Vector2 GetCoverage(Material material)
            => new Vector2(GetFloatOrDefault(material, WIDTH_PROPERTY, 1f), GetFloatOrDefault(material, HEIGHT_PROPERTY, 1f));
    }
}