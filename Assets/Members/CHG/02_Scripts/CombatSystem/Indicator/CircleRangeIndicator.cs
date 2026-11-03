using UnityEngine;

namespace CHG._02_Scripts.CombatSystem.Indicator
{
    public class CircleRangeIndicator : AbstractQuadRangeIndicator
    {
        private const string EDGE_PROPERTY = "_OutLineEdge";
        
        protected override Vector2 GetCoverage(Material material)
        {
            float diameterRatio = GetFloatOrDefault(material, EDGE_PROPERTY, 0.5f) * 2f;
            return new Vector2(diameterRatio, diameterRatio);
        }
    }
}